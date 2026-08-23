using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyEpitaph;

internal class EpitaphSkill
{
    public string label = string.Empty;
    public int level;
    public Passion passion;
    public bool disabled;
}

internal class EpitaphRelation
{
    public string label = string.Empty;
    public string name = string.Empty;
    public FallenColonist? entry;
    public float importance;
}

internal class EpitaphDeed
{
    public string label = string.Empty;
    public string value = string.Empty;
}

internal class EpitaphColonistDetail
{
    private const int MaxRelations = 8;
    private const int MaxDeeds = 8;

    public FallenColonist fallen;
    public string fullName = string.Empty;
    public string ageLine = string.Empty;
    public string honorLine = string.Empty;
    public string statusLine = string.Empty;
    public string timeInColonyLine = string.Empty;
    public string deathLine = string.Empty;
    public string childhood = string.Empty;
    public string adulthood = string.Empty;
    public string traitLine = string.Empty;
    public readonly List<EpitaphSkill> skills = new List<EpitaphSkill>();
    public readonly List<EpitaphRelation> relations = new List<EpitaphRelation>();
    public readonly List<EpitaphDeed> deeds = new List<EpitaphDeed>();

    private EpitaphColonistDetail(FallenColonist fallen)
    {
        this.fallen = fallen;
    }

    public bool HasBackstory => !childhood.NullOrEmpty() || !adulthood.NullOrEmpty();

    public static EpitaphColonistDetail Build(FallenColonist fallen, List<FallenColonist>? roster)
    {
        EpitaphColonistDetail detail = new EpitaphColonistDetail(fallen)
        {
            fullName = fallen.name,
            honorLine = fallen.HonorLabel.Replace("\n", " · "),
            statusLine = fallen.status,
        };

        Pawn? pawn = fallen.pawn;
        if (pawn == null)
        {
            return detail;
        }

        detail.fullName = FullName(pawn, fallen);
        detail.ageLine = AgeLine(pawn);
        detail.timeInColonyLine = TimeInColonyLine(fallen);
        detail.deathLine = DeathLine(fallen);
        FillBackstory(detail, pawn);
        FillTraits(detail, pawn);
        FillSkills(detail, pawn);
        FillRelations(detail, pawn, roster);
        FillDeeds(detail, pawn);
        return detail;
    }

    private static string FullName(Pawn pawn, FallenColonist fallen)
    {
        try
        {
            string? full = pawn.Name?.ToStringFull;
            return full.NullOrEmpty() ? fallen.name : full!;
        }
        catch (Exception)
        {
            return fallen.name;
        }
    }

    private static string AgeLine(Pawn pawn)
    {
        try
        {
            Pawn_AgeTracker? age = pawn.ageTracker;
            if (age == null)
            {
                return string.Empty;
            }

            string gender = pawn.gender != Gender.None ? pawn.gender.GetLabel(pawn.AnimalOrWildMan()) : string.Empty;
            string line = gender.NullOrEmpty()
                ? "Epitaph_DetailAgeOnly".Translate(age.AgeBiologicalYears)
                : "Epitaph_DetailAge".Translate(age.AgeBiologicalYears, gender.CapitalizeFirst());
            if (age.AgeChronologicalYears > age.AgeBiologicalYears + 1)
            {
                line += " " + "Epitaph_DetailChronological".Translate(age.AgeChronologicalYears);
            }

            return line;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string TimeInColonyLine(FallenColonist fallen)
    {
        if (fallen.timeInColony < GenDate.TicksPerDay)
        {
            return string.Empty;
        }

        try
        {
            string period = GenDate.ToStringTicksToPeriod((int)fallen.timeInColony, allowSeconds: false);
            return "Epitaph_DetailTimeInColony".Translate(period);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string DeathLine(FallenColonist fallen)
    {
        if (!fallen.dead)
        {
            return string.Empty;
        }

        if (fallen.deathDate.NullOrEmpty())
        {
            return fallen.status;
        }

        string line = "Epitaph_DetailDied".Translate(fallen.deathDate);
        if (!fallen.deathCause.NullOrEmpty())
        {
            line += " · " + fallen.deathCause;
        }

        return line;
    }

    private static void FillBackstory(EpitaphColonistDetail detail, Pawn pawn)
    {
        try
        {
            Pawn_StoryTracker? story = pawn.story;
            if (story == null)
            {
                return;
            }

            if (story.Childhood != null)
            {
                detail.childhood = story.Childhood.TitleCapFor(pawn.gender);
            }

            if (story.Adulthood != null)
            {
                detail.adulthood = story.Adulthood.TitleCapFor(pawn.gender);
            }
        }
        catch (Exception)
        {
            // Backstory data can be missing on odd pawns.
        }
    }

    private static void FillTraits(EpitaphColonistDetail detail, Pawn pawn)
    {
        try
        {
            List<Trait>? traits = pawn.story?.traits?.TraitsSorted;
            if (traits == null || traits.Count == 0)
            {
                return;
            }

            List<string> labels = new List<string>();
            for (int i = 0; i < traits.Count; i++)
            {
                Trait trait = traits[i];
                if (trait == null || trait.Suppressed)
                {
                    continue;
                }

                labels.Add(trait.LabelCap);
            }

            detail.traitLine = string.Join(", ", labels);
        }
        catch (Exception)
        {
            // Trait data can be missing on odd pawns.
        }
    }

    private static void FillSkills(EpitaphColonistDetail detail, Pawn pawn)
    {
        try
        {
            List<SkillRecord>? records = pawn.skills?.skills;
            if (records == null)
            {
                return;
            }

            List<SkillRecord> ordered = new List<SkillRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i]?.def != null)
                {
                    ordered.Add(records[i]);
                }
            }

            ordered.Sort(CompareSkills);
            for (int i = 0; i < ordered.Count; i++)
            {
                SkillRecord record = ordered[i];
                detail.skills.Add(new EpitaphSkill
                {
                    label = record.def.skillLabel.NullOrEmpty() ? record.def.LabelCap : record.def.skillLabel.CapitalizeFirst(),
                    level = record.Level,
                    passion = record.passion,
                    disabled = record.TotallyDisabled,
                });
            }
        }
        catch (Exception)
        {
            // Skill data can be missing on odd pawns.
        }
    }

    // Vanilla lists skills by descending listOrder: Shooting first, Intellectual last.
    private static int CompareSkills(SkillRecord a, SkillRecord b)
    {
        return b.def.listOrder.CompareTo(a.def.listOrder);
    }

    // Close family such as children and siblings are implied relations rather than direct ones,
    // so they only surface by asking the relation workers about each candidate pawn.
    private static void FillRelations(EpitaphColonistDetail detail, Pawn pawn, List<FallenColonist>? roster)
    {
        try
        {
            Pawn_RelationsTracker? relations = pawn.relations;
            if (relations == null)
            {
                return;
            }

            List<Pawn> candidates = new List<Pawn>();
            HashSet<Pawn> seen = new HashSet<Pawn>();

            void AddCandidate(Pawn? other)
            {
                if (other != null && other != pawn && seen.Add(other))
                {
                    candidates.Add(other);
                }
            }

            if (roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    AddCandidate(roster[i].pawn);
                }
            }

            try
            {
                foreach (Pawn related in relations.PotentiallyRelatedPawns)
                {
                    AddCandidate(related);
                }
            }
            catch (Exception)
            {
                // Keep whatever the walk found before it hit a discarded pawn.
            }

            try
            {
                foreach (Pawn child in relations.Children)
                {
                    AddCandidate(child);
                }
            }
            catch (Exception)
            {
                // Keep whatever the walk found before it hit a discarded pawn.
            }

            List<EpitaphRelation> found = new List<EpitaphRelation>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn other = candidates[i];
                FallenColonist? entry = FindEntry(roster, other);
                if (entry == null && !other.EverSeenByPlayer)
                {
                    continue;
                }

                try
                {
                    PawnRelationDef? def = PawnRelationUtility.GetMostImportantRelation(pawn, other);
                    if (def == null)
                    {
                        continue;
                    }

                    found.Add(new EpitaphRelation
                    {
                        label = RelationLabel(def, other),
                        name = other.Name?.ToStringShort ?? other.LabelShortCap,
                        entry = entry,
                        importance = def.importance,
                    });
                }
                catch (Exception)
                {
                    // Skip the odd pawn whose relation cannot be resolved.
                }
            }

            found.Sort(CompareRelations);
            int shown = Math.Min(found.Count, MaxRelations);
            for (int i = 0; i < shown; i++)
            {
                detail.relations.Add(found[i]);
            }
        }
        catch (Exception)
        {
            // Relationship data can be missing after a wipe.
        }
    }

    private static int CompareRelations(EpitaphRelation a, EpitaphRelation b)
    {
        int importance = b.importance.CompareTo(a.importance);
        if (importance != 0)
        {
            return importance;
        }

        return string.CompareOrdinal(a.name, b.name);
    }

    private static string RelationLabel(PawnRelationDef def, Pawn other)
    {
        if (other.gender == Gender.Female && !def.labelFemale.NullOrEmpty())
        {
            return def.labelFemale.CapitalizeFirst();
        }

        return def.label.NullOrEmpty() ? def.defName : def.label.CapitalizeFirst();
    }

    private static FallenColonist? FindEntry(List<FallenColonist>? roster, Pawn pawn)
    {
        if (roster == null)
        {
            return null;
        }

        for (int i = 0; i < roster.Count; i++)
        {
            if (roster[i].pawn == pawn)
            {
                return roster[i];
            }
        }

        return null;
    }

    private static void FillDeeds(EpitaphColonistDetail detail, Pawn pawn)
    {
        Pawn_RecordsTracker? records = pawn.records;
        if (records == null)
        {
            return;
        }

        RecordDef?[] notable =
        {
            RecordDefOf.Kills,
            RecordDefOf.PawnsDowned,
            RecordDefOf.TimesInMentalState,
            RecordDefOf.OperationsPerformed,
            RecordDefOf.TimesTendedOther,
            RecordDefOf.PrisonersRecruited,
            RecordDefOf.AnimalsTamed,
            RecordDefOf.MealsCooked,
            RecordDefOf.ThingsCrafted,
            RecordDefOf.ThingsConstructed,
            RecordDefOf.ResearchPointsResearched,
            RecordDefOf.CellsMined,
            RecordDefOf.PlantsHarvested,
            RecordDefOf.CorpsesBuried,
        };

        for (int i = 0; i < notable.Length && detail.deeds.Count < MaxDeeds; i++)
        {
            RecordDef? def = notable[i];
            if (def == null)
            {
                continue;
            }

            try
            {
                float value = records.GetValue(def);
                if (value < 1f)
                {
                    continue;
                }

                detail.deeds.Add(new EpitaphDeed
                {
                    label = def.LabelCap,
                    value = Math.Round(value).ToString("N0"),
                });
            }
            catch (Exception)
            {
                // Individual records can be missing; keep collecting the rest.
            }
        }
    }
}
