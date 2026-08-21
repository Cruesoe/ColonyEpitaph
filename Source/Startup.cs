using HarmonyLib;
using Verse;

namespace ColonyEpitaph;

[StaticConstructorOnStartup]
public static class Startup
{
    static Startup()
    {
        new Harmony("cruesoe.colonyepitaph").PatchAll();
    }
}
