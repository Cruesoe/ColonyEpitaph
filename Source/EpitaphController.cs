using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyEpitaph;

public static class EpitaphController
{
    private const float DeathLetterLookDelaySeconds = 6f;

    public static GameOverSnapshot? LastSnapshot { get; private set; }

    private static ChoiceLetter? pendingLetter;
    private static float showAtRealtime = -1f;

    public static bool UseVanillaLetter =>
        ColonyEpitaphMod.Settings != null && ColonyEpitaphMod.Settings.useVanillaLetter;

    public static bool ShouldIntercept(Letter? letter)
    {
        return letter != null && letter.def == LetterDefOf.GameEnded && !UseVanillaLetter && !IsWanderersFollowup(letter);
    }

    public static void DropWanderersLetter(ChoiceLetter? original, int fallbackArrivalTick)
    {
        try
        {
            if (Find.LetterStack == null)
            {
                return;
            }

            ChoiceLetter wanderers = LetterMaker.MakeLetter(
                "GameOverCreateNewWanderers".Translate(),
                "GameOverCreateNewWanderersText".Translate(),
                LetterDefOf.GameEnded);
            Find.LetterStack.ReceiveLetter(wanderers);
            wanderers.arrivalTick = original != null ? original.arrivalTick : fallbackArrivalTick;
            if (original != null && Find.LetterStack.LettersListForReading.Contains(original))
            {
                Find.LetterStack.RemoveLetter(original);
            }
        }
        catch (Exception e)
        {
            Log.Warning("[Colony Epitaph] Could not drop the create-new-wanderers letter.\n" + e);
        }
    }

    private static bool IsWanderersFollowup(Letter letter)
    {
        if (letter is not ChoiceLetter choice)
        {
            return false;
        }

        string expected = "GameOverCreateNewWanderers".Translate().RawText;
        if (expected.NullOrEmpty())
        {
            return false;
        }

        if (!choice.title.NullOrEmpty() && choice.title == expected)
        {
            return true;
        }

        return choice.Label.RawText == expected;
    }

    public static void Show(ChoiceLetter? letter, bool debugPreview = false, bool delay = true)
    {
        if (Find.WindowStack.IsOpen<Dialog_Epitaph>())
        {
            Dialog_Epitaph open = Find.WindowStack.WindowOfType<Dialog_Epitaph>();
            open.UpdateLetter(letter);
            return;
        }

        if (debugPreview || !delay)
        {
            ShowImmediate(letter, debugPreview);
            return;
        }

        pendingLetter = letter;
        if (showAtRealtime < 0f)
        {
            showAtRealtime = Time.realtimeSinceStartup + DeathLetterLookDelaySeconds;
        }
    }

    public static void TickRealtime()
    {
        if (Current.ProgramState != ProgramState.Playing)
        {
            pendingLetter = null;
            showAtRealtime = -1f;
            return;
        }

        if (showAtRealtime < 0f || Time.realtimeSinceStartup < showAtRealtime)
        {
            return;
        }

        ChoiceLetter? letter = pendingLetter;
        pendingLetter = null;
        showAtRealtime = -1f;
        ShowImmediate(letter, debugPreview: false);
    }

    private static void ShowImmediate(ChoiceLetter? letter, bool debugPreview)
    {
        if (Find.WindowStack.IsOpen<Dialog_Epitaph>())
        {
            Find.WindowStack.WindowOfType<Dialog_Epitaph>().UpdateLetter(letter);
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
