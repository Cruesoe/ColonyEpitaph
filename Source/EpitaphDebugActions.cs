using LudeonTK;
using Verse;

namespace ColonyEpitaph;

public static class EpitaphDebugActions
{
    [DebugAction("Colony Epitaph", "Open epitaph screen", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void OpenEpitaphScreen()
    {
        EpitaphController.Show(null, debugPreview: true);
    }
}
