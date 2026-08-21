using System;
using RimWorld;
using Verse;

namespace ColonyEpitaph;

public static class EpitaphController
{
    public static GameOverSnapshot? LastSnapshot { get; private set; }

    public static bool UseVanillaLetter =>
        ColonyEpitaphMod.Settings != null && ColonyEpitaphMod.Settings.useVanillaLetter;

    public static void Show(ChoiceLetter? letter, bool debugPreview = false)
    {
        if (Find.WindowStack.IsOpen<Dialog_Epitaph>())
        {
            Dialog_Epitaph open = Find.WindowStack.WindowOfType<Dialog_Epitaph>();
            open.UpdateLetter(letter);
            return;
        }

        GameOverSnapshot snapshot;
        if (debugPreview || LastSnapshot == null)
        {
            snapshot = GameOverSnapshot.Capture(letter, debugPreview);
            if (!debugPreview)
            {
                LastSnapshot = snapshot;
            }
        }
        else
        {
            snapshot = LastSnapshot;
        }

        Find.WindowStack.Add(new Dialog_Epitaph(snapshot, letter));
    }
}
