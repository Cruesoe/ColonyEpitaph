using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyEpitaph;

[HarmonyPatch(typeof(LetterStack), nameof(LetterStack.ReceiveLetter), typeof(Letter), typeof(string), typeof(int), typeof(bool))]
public static class Patch_LetterStack_ReceiveLetter
{
    public static void Postfix(Letter let)
    {
        if (let == null || let.def != LetterDefOf.GameEnded || EpitaphController.UseVanillaLetter)
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
        if (__instance.def != LetterDefOf.GameEnded || EpitaphController.UseVanillaLetter)
        {
            return true;
        }

        EpitaphController.Show(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(Letter), "get_CanShowInLetterStack")]
public static class Patch_Letter_CanShowInLetterStack
{
    public static void Postfix(Letter __instance, ref bool __result)
    {
        if (!__result || __instance.def != LetterDefOf.GameEnded || EpitaphController.UseVanillaLetter)
        {
            return;
        }

        __result = false;
    }
}
