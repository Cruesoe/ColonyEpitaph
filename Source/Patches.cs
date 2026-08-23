using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ColonyEpitaph;

[HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter), typeof(Letter), typeof(string), typeof(int), typeof(bool))]
public static class Patch_LetterStack_ReceiveLetter
{
    public static void Postfix(Letter let)
    {
        if (!EpitaphController.ShouldIntercept(let))
        {
            return;
        }

        try
        {
            EpitaphController.Show(let as ChoiceLetter);
        }
        catch (System.Exception e)
        {
            Log.Error("[Colony Epitaph] Failed to open the epitaph screen.\n" + e);
        }
    }
}

[HarmonyPatch(typeof(ChoiceLetter), nameof(ChoiceLetter.OpenLetter))]
public static class Patch_ChoiceLetter_OpenLetter
{
    public static bool Prefix(ChoiceLetter __instance)
    {
        if (!EpitaphController.ShouldIntercept(__instance))
        {
            return true;
        }

        EpitaphController.Show(__instance, delay: false);
        return false;
    }
}

[HarmonyPatch(typeof(Letter), "get_CanShowInLetterStack")]
public static class Patch_Letter_CanShowInLetterStack
{
    public static void Postfix(Letter __instance, ref bool __result)
    {
        if (!__result || !EpitaphController.ShouldIntercept(__instance))
        {
            return;
        }

        // ReceiveLetter bails out if this is false *before* the letter is added.
        // Only hide the icon after it is actually on the stack.
        if (Find.LetterStack.LettersListForReading.Contains(__instance))
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootUpdate))]
public static class Patch_UIRoot_Play_UIRootUpdate
{
    public static void Postfix()
    {
        EpitaphController.TickRealtime();
    }
}

[HarmonyPatch(typeof(MusicManagerPlay), "get_CurVolume")]
public static class Patch_MusicManagerPlay_CurVolume
{
    public static void Postfix(ref float __result)
    {
        float volume = EpitaphAudio.Volume;
        if (volume < 0.999f)
        {
            __result *= volume;
        }
    }
}

[HarmonyPatch(typeof(Sample), "get_Volume")]
public static class Patch_Sample_Volume
{
    public static void Postfix(Sample __instance, ref float __result)
    {
        float volume = EpitaphAudio.Volume;
        if (volume >= 0.999f || __result <= 0f)
        {
            return;
        }

        try
        {
            if (__instance.IsAmbient)
            {
                __result *= volume;
            }
        }
        catch (System.Exception)
        {
            // Ambient detection can fail on a torn-down sample; leave its volume alone.
        }
    }
}
