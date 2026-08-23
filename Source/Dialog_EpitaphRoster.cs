using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ColonyEpitaph;

public class Dialog_EpitaphRoster : Window
{
    private const float PortraitGap = 16f;
    private const float HeaderHeight = 36f;

    private readonly List<FallenColonist> remaining;
    private readonly List<FallenColonist>? roster;
    private Vector2 scroll;

    public override Vector2 InitialSize => new Vector2(760f, 560f);

    public Dialog_EpitaphRoster(List<FallenColonist> remaining, List<FallenColonist>? roster = null)
    {
        this.remaining = remaining ?? new List<FallenColonist>();
        this.roster = roster;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
        doCloseButton = false;
        closeOnClickedOutside = true;
        closeOnAccept = false;
        closeOnCancel = true;
        preventCameraMotion = true;
        layer = WindowLayer.Super;
        ignoreScreenFader = true;
        onlyOneOfTypeAllowed = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperCenter;
        GUI.color = new Color(0.92f, 0.86f, 0.74f);
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight),
            "Epitaph_Remaining".Translate(remaining.Count));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        Rect view = new Rect(inRect.x, inRect.y + HeaderHeight + 8f, inRect.width, inRect.height - HeaderHeight - 8f);
        float slotWidth = EpitaphColonistCard.PortraitSize + PortraitGap;
        float slotHeight = EpitaphColonistCard.SlotHeight(EpitaphColonistCard.PortraitSize, compact: false);
        int columns = Mathf.Max(1, Mathf.FloorToInt((view.width - 16f) / slotWidth));
        int rows = Mathf.Max(1, Mathf.CeilToInt(remaining.Count / (float)columns));
        Rect contents = new Rect(0f, 0f, view.width - 16f, Math.Max(rows * slotHeight, view.height));
        bool interactive = !Dialog_EpitaphColonistDetail.AnyOpen;
        Widgets.BeginScrollView(view, ref scroll, contents);
        try
        {
            for (int i = 0; i < remaining.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Rect slot = new Rect(col * slotWidth, row * slotHeight, EpitaphColonistCard.PortraitSize, slotHeight);
                EpitaphColonistCard.Draw(slot, remaining[i], 1f, interactive, roster ?? remaining);
            }
        }
        finally
        {
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
