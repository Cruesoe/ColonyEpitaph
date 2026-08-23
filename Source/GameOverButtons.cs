using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyEpitaph;

public static class GameOverButtons
{
    public static List<(string label, bool disabled, string? disabledReason, Action? action)> Build(ChoiceLetter? letter, int fallbackArrivalTick)
    {
        List<(string, bool, string?, Action?)> buttons = new List<(string, bool, string?, Action?)>();
        bool canSpawn = false;
        try
        {
            canSpawn = Find.GameEnder != null && Find.GameEnder.CanSpawnNewWanderers();
        }
        catch (Exception)
        {
            canSpawn = false;
        }

        if (canSpawn)
        {
            buttons.Add(("GameOverKeepWatchingForNow".Translate(), false, null, null));
            AcceptanceReport canCreate = CanCreateNewWanderers();
            int arrivalTick = letter?.arrivalTick ?? fallbackArrivalTick;
            buttons.Add((
                "GameOverCreateNewWanderers".Translate(),
                !canCreate.Accepted,
                canCreate.Reason,
                () => EpitaphController.DropWanderersLetter(letter, arrivalTick)));
        }
        else
        {
            buttons.Add(("GameOverKeepWatching".Translate(), false, null, null));
        }

        buttons.Add(("GameOverMainMenu".Translate(), false, null, GenScene.GoToMainMenu));
        return buttons;
    }

    private static AcceptanceReport CanCreateNewWanderers()
    {
        if (Current.Game == null)
        {
            return false;
        }

        bool anyHome = false;
        foreach (Map map in Current.Game.PlayerHomeMaps)
        {
            if (map.Tile.Layer.IsRootSurface)
            {
                return true;
            }

            anyHome = true;
        }

        if (anyHome)
        {
            return "NoWandererDestination".Translate();
        }

        return "NoColony".Translate();
    }
}
