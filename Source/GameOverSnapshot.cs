using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyEpitaph;

public class FallenColonist
{
    public Pawn? pawn;
    public string name = string.Empty;
    public string status = string.Empty;
    public string roleLabel = string.Empty;
    public Color roleColor = new Color(0.92f, 0.86f, 0.74f);
    public bool hasRole;
    public bool isLeader;
    public bool hasRoyalTitle;
    public string royalTitleLabel = string.Empty;
    public int royalSeniority;
    public bool dead;
    public bool kidnapped;
    public int timeOfDeath;
    public float timeInColony;
    public string deathDate = string.Empty;
    public string deathCause = string.Empty;

    public bool Featured => hasRole || hasRoyalTitle;

    public Color HonorBorder => hasRoyalTitle ? new Color(0.90f, 0.75f, 0.28f) : roleColor;

    public string HonorLabel
    {
        get
        {
            if (hasRoyalTitle && !royalTitleLabel.NullOrEmpty() && hasRole && !roleLabel.NullOrEmpty())
            {
                return royalTitleLabel + "\n" + roleLabel;
            }

            if (hasRoyalTitle && !royalTitleLabel.NullOrEmpty())
            {
                return royalTitleLabel;
            }

            return roleLabel;
        }
    }
}

public class GameOverSnapshot
{
    public string colonyName = string.Empty;
    public string heading = string.Empty;
    public string subtitle = string.Empty;
    public string lastedText = string.Empty;
    public string statsText = string.Empty;
    public int gameOverTick;
    public bool debugPreview;
    public bool permadeath;
    public readonly List<FallenColonist> fallen = new List<FallenColonist>();

    public static GameOverSnapshot Capture(ChoiceLetter? letter, bool debugPreview = false)
    {
        GameOverSnapshot snapshot = new GameOverSnapshot
        {
            debugPreview = debugPreview,
            gameOverTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0,
            permadeath = Find.GameInfo != null && Find.GameInfo.permadeathMode,
            colonyName = ColonyName(),
            heading = "Epitaph_Heading".Translate(),
        };

        if (letter != null && !letter.Text.NullOrEmpty())
        {
            snapshot.subtitle = letter.Text.Resolve();
        }
        else if (debugPreview)
        {
            snapshot.subtitle = "Epitaph_DebugSubtitle".Translate();
        }
        else
        {
            snapshot.subtitle = "Epitaph_EveryoneGone".Translate();
        }

        snapshot.lastedText = BuildLastedText(snapshot.gameOverTick);
        snapshot.statsText = BuildStatsText();

        CollectFallen(snapshot, debugPreview);
        return snapshot;
    }

    private static string ColonyName()
    {
        try
        {
            string? name = Faction.OfPlayerSilentFail?.Name;
            if (!name.NullOrEmpty())
            {
                return name!;
            }
        }
        catch (Exception)
        {
            // Faction data can be missing in odd end states.
        }

        return "Epitaph_UnnamedColony".Translate();
    }

    private static string BuildLastedText(int ticksGame)
    {
        string lasted = GenDate.ToStringTicksToPeriod(ticksGame, allowSeconds: false);
        string? fellOn = DateColonyFell();
        if (!fellOn.NullOrEmpty())
        {
            return "Epitaph_LastedUntil".Translate(lasted, fellOn);
        }

        return "Epitaph_Lasted".Translate(lasted);
    }

    private static string? DateColonyFell()
    {
        try
        {
            TickManager? ticks = Find.TickManager;
            WorldGrid? grid = Find.WorldGrid;
            if (ticks == null || grid == null)
            {
                return null;
            }

            return GenDate.DateFullStringAt(ticks.TicksAbs, LongLatForDate(grid));
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not read the date the colony fell.\n" + e);
            return null;
        }
    }

    private static Vector2 LongLatForDate(WorldGrid grid)
    {
        Map? map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
        if (map != null && map.Tile.Valid)
        {
            return grid.LongLatOf(map.Tile);
        }

        try
        {
            List<Settlement>? settlements = Find.WorldObjects?.Settlements;
            if (settlements != null)
            {
                for (int i = 0; i < settlements.Count; i++)
                {
                    Settlement settlement = settlements[i];
                    if (settlement != null && settlement.Faction == Faction.OfPlayerSilentFail && settlement.Tile.Valid)
                    {
                        return grid.LongLatOf(settlement.Tile);
                    }
                }
            }
        }
        catch (Exception)
        {
            // World objects can be missing in odd end states.
        }

        return Vector2.zero;
    }

    private static string BuildStatsText()
    {
        List<string> parts = new List<string>();

        try
        {
            int currentWealth = (int)Math.Round(CurrentColonyWealth());
            int peakWealth = (int)Math.Round(Math.Max(currentWealth, PeakRecordedWealth()));
            string wealthNumber = currentWealth.ToString("N0");
            if (peakWealth > currentWealth)
            {
                parts.Add("Epitaph_WealthWithPeak".Translate(wealthNumber, peakWealth.ToString("N0")));
            }
            else
            {
                parts.Add("Epitaph_Wealth".Translate(wealthNumber));
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not read colony wealth for the epitaph.\n" + e);
        }

        try
        {
            StatsRecord? stats = Find.StoryWatcher?.statsRecord;
            if (stats != null)
            {
                if (stats.greatestPopulation > 0)
                {
                    parts.Add("Epitaph_PeakColonists".Translate(stats.greatestPopulation));
                }

                if (stats.numRaidsEnemy > 0)
                {
                    parts.Add("Epitaph_Raids".Translate(stats.numRaidsEnemy));
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not read colony statistics for the epitaph.\n" + e);
        }

        return string.Join(" · ", parts);
    }

    private static float CurrentColonyWealth()
    {
        float wealth = 0f;
        if (Find.Maps == null)
        {
            return wealth;
        }

        foreach (Map map in Find.Maps)
        {
            if (map == null || !map.IsPlayerHome || map.wealthWatcher == null)
            {
                continue;
            }

            try
            {
                map.wealthWatcher.ForceRecount(true);
            }
            catch (Exception)
            {
                // Use the last cached total if a recount fails in a broken end state.
            }

            wealth += map.wealthWatcher.WealthTotal;
        }

        return wealth;
    }

    private static float PeakRecordedWealth()
    {
        History? history = Find.History;
        List<HistoryAutoRecorderGroup>? groups = history?.Groups();
        if (groups == null)
        {
            return 0f;
        }

        HistoryAutoRecorderDef? def = DefDatabase<HistoryAutoRecorderDef>.GetNamedSilentFail("Wealth_Total");
        if (def == null)
        {
            return 0f;
        }

        float peak = 0f;
        foreach (HistoryAutoRecorderGroup group in groups)
        {
            if (group?.recorders == null)
            {
                continue;
            }

            foreach (HistoryAutoRecorder recorder in group.recorders)
            {
                if (recorder?.def != def || recorder.records == null)
                {
                    continue;
                }

                for (int i = 0; i < recorder.records.Count; i++)
                {
                    if (recorder.records[i] > peak)
                    {
                        peak = recorder.records[i];
                    }
                }
            }
        }

        return peak;
    }

    private static void CollectFallen(GameOverSnapshot snapshot, bool debugPreview)
    {
        HashSet<Pawn> seen = new HashSet<Pawn>();

        void TryAdd(Pawn? pawn)
        {
            if (pawn == null || !seen.Add(pawn) || !IsColonyHuman(pawn))
            {
                return;
            }

            FallenColonist fallen = new FallenColonist
            {
                pawn = pawn,
                name = pawn.Name?.ToStringShort ?? pawn.LabelCap,
                dead = pawn.Dead,
                kidnapped = IsKidnapped(pawn),
                timeOfDeath = DeathTick(pawn),
                timeInColony = TimeInColony(pawn),
            };
            fallen.status = StatusFor(fallen, debugPreview);
            if (fallen.dead)
            {
                fallen.deathDate = DeathDate(pawn);
                fallen.deathCause = CauseOfDeath(pawn) ?? string.Empty;
            }

            FillIdeologyRole(fallen, pawn);
            FillRoyalTitle(fallen, pawn);
            snapshot.fallen.Add(fallen);
        }

        try
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists)
            {
                TryAdd(pawn);
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not scan living colonists for the epitaph.\n" + e);
        }

        try
        {
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                TryAdd(pawn);
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not scan map colonists for the epitaph.\n" + e);
        }

        try
        {
            List<Pawn>? kidnapped = Faction.OfPlayerSilentFail?.kidnapped?.KidnappedPawnsListForReading;
            if (kidnapped != null)
            {
                foreach (Pawn pawn in kidnapped)
                {
                    TryAdd(pawn);
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not scan kidnapped colonists for the epitaph.\n" + e);
        }

        try
        {
            if (Find.Maps != null)
            {
                foreach (Map map in Find.Maps)
                {
                    if (map?.listerThings == null)
                    {
                        continue;
                    }

                    foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
                    {
                        if (thing is Corpse corpse)
                        {
                            TryAdd(corpse.InnerPawn);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not scan corpses for the epitaph.\n" + e);
        }

        try
        {
            WorldPawns? worldPawns = Find.WorldPawns;
            if (worldPawns != null)
            {
                foreach (Pawn pawn in worldPawns.AllPawnsAliveOrDead)
                {
                    if (ShouldIncludeWorldPawn(worldPawns, pawn))
                    {
                        TryAdd(pawn);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not scan world pawns for the epitaph.\n" + e);
        }

        try
        {
            foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
            {
                if (pawn != null && pawn.Dead)
                {
                    TryAdd(pawn);
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not scan dead pawns for the epitaph.\n" + e);
        }

        snapshot.fallen.Sort(CompareFallen);
    }

    private static int CompareFallen(FallenColonist a, FallenColonist b)
    {
        int featuredA = a.Featured ? 0 : 1;
        int featuredB = b.Featured ? 0 : 1;
        if (featuredA != featuredB)
        {
            return featuredA.CompareTo(featuredB);
        }

        if (a.Featured && b.Featured)
        {
            int roleA = RoleRank(a);
            int roleB = RoleRank(b);
            if (roleA != roleB)
            {
                return roleA.CompareTo(roleB);
            }

            if (a.hasRoyalTitle && b.hasRoyalTitle && a.royalSeniority != b.royalSeniority)
            {
                return b.royalSeniority.CompareTo(a.royalSeniority);
            }
        }

        int time = b.timeInColony.CompareTo(a.timeInColony);
        if (time != 0)
        {
            return time;
        }

        return string.CompareOrdinal(a.name, b.name);
    }

    private static int RoleRank(FallenColonist fallen)
    {
        if (fallen.isLeader)
        {
            return 0;
        }

        if (fallen.hasRoyalTitle)
        {
            return 1;
        }

        if (fallen.hasRole)
        {
            return 2;
        }

        return 3;
    }

    private static void FillIdeologyRole(FallenColonist fallen, Pawn pawn)
    {
        if (!ModsConfig.IdeologyActive)
        {
            return;
        }

        try
        {
            Precept_Role? role = FindRole(pawn);
            if (role == null)
            {
                return;
            }

            fallen.hasRole = true;
            fallen.isLeader = role.def != null && role.def.leaderRole;
            string label = role.LabelForPawn(pawn);
            if (label.NullOrEmpty())
            {
                label = role.LabelCap;
            }

            fallen.roleLabel = label ?? string.Empty;
            fallen.roleColor = role.LabelColor;
        }
        catch (Exception)
        {
            // Ideology data can be missing after a wipe.
        }
    }

    private static void FillRoyalTitle(FallenColonist fallen, Pawn pawn)
    {
        if (!ModsConfig.RoyaltyActive || pawn.royalty == null)
        {
            return;
        }

        try
        {
            RoyalTitle? title = pawn.royalty.MostSeniorTitle;
            if (title?.def == null)
            {
                return;
            }

            fallen.hasRoyalTitle = true;
            fallen.royalSeniority = title.def.seniority;
            string label = title.def.GetLabelCapFor(pawn);
            if (label.NullOrEmpty())
            {
                label = title.Label;
            }

            fallen.royalTitleLabel = label ?? string.Empty;
        }
        catch (Exception)
        {
            // Royalty data can be missing after a wipe.
        }
    }

    private static Precept_Role? FindRole(Pawn pawn)
    {
        Precept_Role? role = pawn.Ideo?.GetRole(pawn);
        if (role != null)
        {
            return role;
        }

        IEnumerable<Ideo>? ideos = Faction.OfPlayerSilentFail?.ideos?.AllIdeos;
        if (ideos == null)
        {
            return null;
        }

        foreach (Ideo ideo in ideos)
        {
            role = ideo?.GetRole(pawn);
            if (role != null)
            {
                return role;
            }
        }

        return null;
    }

    private static bool IsColonyHuman(Pawn pawn)
    {
        if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike || pawn.IsSubhuman)
        {
            return false;
        }

        if (IsKidnapped(pawn))
        {
            return true;
        }

        if (pawn.IsColonist || pawn.IsFreeColonist)
        {
            return true;
        }

        if (!pawn.Dead)
        {
            return false;
        }

        if (pawn.Faction == Faction.OfPlayerSilentFail)
        {
            return true;
        }

        return pawn.EverSeenByPlayer && HasColonistTime(pawn);
    }

    private static bool HasColonistTime(Pawn pawn)
    {
        return TimeInColony(pawn) > 0f;
    }

    private static float TimeInColony(Pawn pawn)
    {
        try
        {
            Pawn_RecordsTracker? records = pawn.records;
            if (records == null)
            {
                return 0f;
            }

            float time = 0f;
            if (RecordDefOf.TimeAsColonistOrColonyAnimal != null)
            {
                time += records.GetValue(RecordDefOf.TimeAsColonistOrColonyAnimal);
            }

            if (RecordDefOf.TimeAsChildInColony != null)
            {
                time += records.GetValue(RecordDefOf.TimeAsChildInColony);
            }

            return time;
        }
        catch (Exception)
        {
            return 0f;
        }
    }

    private static int DeathTick(Pawn pawn)
    {
        if (pawn.Corpse != null)
        {
            return pawn.Corpse.timeOfDeath;
        }

        return pawn.becameWorldPawnTickAbs;
    }

    private static string DeathDate(Pawn pawn)
    {
        try
        {
            int absTicks = DeathTickAbs(pawn);
            WorldGrid? grid = Find.WorldGrid;
            if (absTicks <= 0 || grid == null)
            {
                return string.Empty;
            }

            return GenDate.DateFullStringAt(absTicks, LongLatForDate(grid));
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    // Corpse.Age is measured in the same base as the running game, so subtracting it from
    // TicksAbs avoids mixing game ticks with absolute ticks.
    private static int DeathTickAbs(Pawn pawn)
    {
        TickManager? ticks = Find.TickManager;
        if (pawn.Corpse != null && ticks != null)
        {
            return ticks.TicksAbs - pawn.Corpse.Age;
        }

        return pawn.becameWorldPawnTickAbs;
    }

    private static bool ShouldIncludeWorldPawn(WorldPawns worldPawns, Pawn pawn)
    {
        switch (worldPawns.GetSituation(pawn))
        {
            case WorldPawnSituation.Kidnapped:
            case WorldPawnSituation.CaravanMember:
            case WorldPawnSituation.InTravelingTransportPod:
            case WorldPawnSituation.Teleporting:
                return true;
            case WorldPawnSituation.Dead:
                return pawn.EverSeenByPlayer && HasColonistTime(pawn);
            default:
                return false;
        }
    }

    private static bool IsKidnapped(Pawn pawn)
    {
        List<Pawn>? kidnapped = Faction.OfPlayerSilentFail?.kidnapped?.KidnappedPawnsListForReading;
        return kidnapped != null && kidnapped.Contains(pawn);
    }

    private static string StatusFor(FallenColonist fallen, bool debugPreview)
    {
        if (debugPreview && fallen.pawn != null && !fallen.pawn.Dead && !fallen.kidnapped)
        {
            return "Epitaph_AliveDebug".Translate();
        }

        if (fallen.dead)
        {
            string? cause = CauseOfDeath(fallen.pawn);
            if (!cause.NullOrEmpty())
            {
                return "Epitaph_DiedOf".Translate(cause);
            }

            return "Epitaph_Dead".Translate();
        }

        if (fallen.kidnapped)
        {
            return "Epitaph_Kidnapped".Translate();
        }

        if (fallen.pawn != null && !fallen.pawn.Spawned && fallen.pawn.MapHeld == null && !fallen.pawn.Dead)
        {
            return "Epitaph_Left".Translate();
        }

        return "Epitaph_Missing".Translate();
    }

    private static string? CauseOfDeath(Pawn? pawn)
    {
        HediffSet? hediffs = pawn?.health?.hediffSet;
        if (hediffs?.hediffs == null)
        {
            return null;
        }

        Hediff? lethal = null;
        for (int i = 0; i < hediffs.hediffs.Count; i++)
        {
            Hediff hediff = hediffs.hediffs[i];
            if (hediff == null)
            {
                continue;
            }

            bool killing = false;
            try
            {
                killing = hediff.CauseDeathNow();
            }
            catch (Exception)
            {
                killing = hediff.def != null && hediff.def.lethalSeverity > 0f && hediff.Severity >= hediff.def.lethalSeverity;
            }

            if (killing)
            {
                lethal = hediff;
            }
        }

        if (lethal == null)
        {
            return null;
        }

        if (!lethal.sourceLabel.NullOrEmpty())
        {
            return lethal.sourceLabel.CapitalizeFirst();
        }

        return lethal.LabelCap;
    }
}
