using UnityEngine;
using Verse;

namespace ColonyEpitaph;

public class ColonyEpitaphMod : Mod
{
    public static ColonyEpitaphSettings Settings = null!;

    public ColonyEpitaphMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<ColonyEpitaphSettings>();
    }

    public override string SettingsCategory()
    {
        return "Epitaph_SettingsCategory".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoWindowContents(inRect);
        base.DoSettingsWindowContents(inRect);
    }
}

public class ColonyEpitaphSettings : ModSettings
{
    public bool useVanillaLetter;

    public void DoWindowContents(Rect inRect)
    {
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled(
            "Epitaph_UseVanillaLetter".Translate(),
            ref useVanillaLetter,
            "Epitaph_UseVanillaLetterTip".Translate());
        listing.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref useVanillaLetter, "useVanillaLetter", false);
    }
}
