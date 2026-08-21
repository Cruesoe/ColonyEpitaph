using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyEpitaph;

public class FallenColonist
{
    public Pawn? pawn;
    public string name = string.Empty;
    public string status = string.Empty;
    public bool dead;
    public bool kidnapped;
    public int timeOfDeath;
}

public class GameOverSnapshot
{
    public const int MaxPortraits = 8;

    public string colonyName = string.Empty;
    public string heading = string.Empty;
    public string subtitle = string.Empty;
    public string lastedText = string.Empty;
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

        int ticks = snapshot.gameOverTick;
        snapshot.lastedText = "Epitaph_Lasted".Translate(GenDate.ToStringTicksToPeriod(ticks, allowSeconds: false));

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
                timeOfDeath = pawn.Corpse != null ? pawn.Corpse.timeOfDeath : 0,
            };
            fallen.status = StatusFor(fallen, debugPreview);
            snapshot.fallen.Add(fallen);
        }

        try
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
            {
                TryAdd(pawn);
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not scan world pawns for the epitaph.\n" + e);
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

        if (debugPreview && snapshot.fallen.Count == 0)
        {
            try
            {
                foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
                {
                    TryAdd(pawn);
                }
            }
            catch (Exception)
            {
                // Preview with no colonists is an empty state the dialog already handles.
            }
        }

        snapshot.fallen.Sort(CompareFallen);
    }

    private static int CompareFallen(FallenColonist a, FallenColonist b)
    {
        int rankA = Rank(a);
        int rankB = Rank(b);
        if (rankA != rankB)
        {
            return rankA.CompareTo(rankB);
        }

        return b.timeOfDeath.CompareTo(a.timeOfDeath);
    }

    private static int Rank(FallenColonist fallen)
    {
        if (fallen.dead)
        {
            return 0;
        }

        if (fallen.kidnapped)
        {
            return 1;
        }

        return 2;
    }

    private static bool IsColonyHuman(Pawn pawn)
    {
        if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
        {
            return false;
        }

        if (pawn.IsColonist || pawn.IsFreeColonist)
        {
            return true;
        }

        if (pawn.Faction == Faction.OfPlayerSilentFail)
        {
            return true;
        }

        return IsKidnapped(pawn);
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
