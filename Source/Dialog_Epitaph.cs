using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyEpitaph;

public class Dialog_Epitaph : Window
{
    private const float PanelWidth = 860f;
    private const float PanelHeight = 640f;
    private const float ButtonHeight = 38f;
    private const float ButtonGap = 8f;
    private const int MaxRolePortraits = 6;
    private const int MaxSmallPortraits = 8;
    private const float RoleSlotGap = 18f;
    private const float SmallSlotGap = 12f;
    private const float SeeMoreWidth = 132f;
    private const float SeeMoreHeight = 30f;
    private const float FadeToBlackSeconds = 2.5f;
    private const float BlackHoldSeconds = 0.4f;
    private const float FadeInSeconds = 1.15f;

    private static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.07f, 0.94f);
    private static readonly Color PanelEdge = new Color(0.32f, 0.28f, 0.22f, 0.9f);
    private static readonly Color TitleColor = new Color(0.92f, 0.86f, 0.74f);
    private static readonly Color MutedColor = new Color(0.72f, 0.68f, 0.62f);

    private readonly GameOverSnapshot snapshot;
    private ChoiceLetter? letter;
    private float openedAt = -1f;

    public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

    protected override float Margin => 0f;

    public Dialog_Epitaph(GameOverSnapshot snapshot, ChoiceLetter? letter)
    {
        this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        this.letter = letter;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = false;
        doCloseButton = false;
        doWindowBackground = false;
        drawShadow = false;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = true;
        preventCameraMotion = true;
        onlyOneOfTypeAllowed = true;
        layer = WindowLayer.Super;
        silenceAmbientSound = false;
        ignoreScreenFader = true;
        closeOnCancel = false;
    }

    private static float FadeTotalSeconds => FadeToBlackSeconds + BlackHoldSeconds + FadeInSeconds;

    private float OverlayAlpha
    {
        get
        {
            if (openedAt < 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01((Time.realtimeSinceStartup - openedAt) / FadeToBlackSeconds);
        }
    }

    private float ContentAlpha
    {
        get
        {
            if (openedAt < 0f)
            {
                return 0f;
            }

            float t = Time.realtimeSinceStartup - openedAt - FadeToBlackSeconds - BlackHoldSeconds;
            return Mathf.Clamp01(t / FadeInSeconds);
        }
    }

    private bool FadeFinished => ContentAlpha >= 0.999f;

    private bool PortraitsInteractive =>
        FadeFinished && Find.WindowStack != null && !Find.WindowStack.IsOpen<Dialog_EpitaphRoster>() &&
        !Dialog_EpitaphColonistDetail.AnyOpen;

    public override void PreOpen()
    {
        base.PreOpen();
        if (openedAt < 0f)
        {
            openedAt = Time.realtimeSinceStartup;
        }

        SyncAudioFade();
    }

    public override void PostClose()
    {
        EpitaphAudio.Volume = 1f;
        base.PostClose();
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        closeOnCancel = FadeFinished;
        SyncAudioFade();
    }

    private void SkipFade()
    {
        openedAt = Time.realtimeSinceStartup - FadeTotalSeconds;
        SyncAudioFade();
    }

    private void SyncAudioFade()
    {
        EpitaphAudio.Volume = 1f - OverlayAlpha;
    }

    public void UpdateLetter(ChoiceLetter? nextLetter)
    {
        if (nextLetter != null)
        {
            letter = nextLetter;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (!FadeFinished && Event.current != null &&
            (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown))
        {
            SkipFade();
            Event.current.Use();
        }

        Widgets.DrawBoxSolid(inRect, new Color(0f, 0f, 0f, OverlayAlpha));
        if (ContentAlpha <= 0.001f)
        {
            return;
        }

        Rect panel = new Rect(
            (inRect.width - PanelWidth) / 2f,
            (inRect.height - PanelHeight) / 2f,
            PanelWidth,
            PanelHeight);
        Color panelFill = PanelColor;
        panelFill.a *= ContentAlpha;
        Color panelEdge = PanelEdge;
        panelEdge.a *= ContentAlpha;
        Widgets.DrawBoxSolidWithOutline(panel, panelFill, panelEdge, 2);
        Rect inner = panel.ContractedBy(28f);

        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        bool oldWrap = Text.WordWrap;

        try
        {
            float y = inner.y;

            Text.Anchor = TextAnchor.UpperCenter;
            Text.WordWrap = true;

            Text.Font = GameFont.Medium;
            GUI.color = WithAlpha(TitleColor, ContentAlpha);
            Rect headingRect = new Rect(inner.x, y, inner.width, 32f);
            Widgets.Label(headingRect, snapshot.heading);
            y += 36f;

            Text.Font = GameFont.Medium;
            GUI.color = WithAlpha(Color.white, ContentAlpha);
            Rect nameRect = new Rect(inner.x, y, inner.width, Text.CalcHeight(snapshot.colonyName, inner.width));
            Widgets.Label(nameRect, snapshot.colonyName);
            y += nameRect.height + 10f;

            Text.Font = GameFont.Small;
            GUI.color = WithAlpha(MutedColor, ContentAlpha);
            if (!snapshot.subtitle.NullOrEmpty())
            {
                float subHeight = Text.CalcHeight(snapshot.subtitle, inner.width);
                Rect subRect = new Rect(inner.x, y, inner.width, subHeight);
                Widgets.Label(subRect, snapshot.subtitle);
                y += subHeight + 8f;
            }

            GUI.color = WithAlpha(TitleColor, ContentAlpha);
            Rect lastedRect = new Rect(inner.x, y, inner.width, 24f);
            Widgets.Label(lastedRect, snapshot.lastedText);
            y += 24f;

            if (!snapshot.statsText.NullOrEmpty())
            {
                GUI.color = WithAlpha(MutedColor, ContentAlpha);
                Rect statsRect = new Rect(inner.x, y, inner.width, 22f);
                Widgets.Label(statsRect, snapshot.statsText);
                y += 22f;
            }

            y += 12f;

            Widgets.DrawLineHorizontal(inner.x + 80f, y, inner.width - 160f, panelEdge);
            y += 18f;

            Rect portraitsRect = new Rect(inner.x, y, inner.width, inner.yMax - y - ButtonHeight - 24f);
            DrawPortraits(portraitsRect);

            Rect buttonsRect = new Rect(inner.x, inner.yMax - ButtonHeight, inner.width, ButtonHeight);
            DrawButtons(buttonsRect);
        }
        finally
        {
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
            Text.WordWrap = oldWrap;
        }
    }

    private void DrawPortraits(Rect rect)
    {
        if (snapshot.fallen.Count == 0)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = WithAlpha(MutedColor, ContentAlpha);
            Widgets.Label(rect, "Epitaph_NoColonists".Translate());
            return;
        }

        List<FallenColonist> featured = new List<FallenColonist>();
        List<FallenColonist> small = new List<FallenColonist>();
        List<FallenColonist> remaining = new List<FallenColonist>();
        SplitPortraitGroups(snapshot, featured, small, remaining);

        bool showSeeAll = remaining.Count > 0;

        float roleHeight = featured.Count > 0 ? EpitaphColonistCard.SlotHeight(EpitaphColonistCard.RolePortraitSize, compact: false) : 0f;
        float smallHeight = small.Count > 0
            ? EpitaphColonistCard.SlotHeight(EpitaphColonistCard.SmallPortraitSize, compact: true)
            : 0f;
        float moreGap = small.Count > 0 && showSeeAll ? 10f : 0f;
        float moreHeight = showSeeAll ? SeeMoreHeight : 0f;
        float gap = featured.Count > 0 && (small.Count > 0 || showSeeAll) ? 10f : 0f;
        float blockHeight = roleHeight + gap + smallHeight + moreGap + moreHeight;
        float y = rect.y + Math.Max(0f, (rect.height - blockHeight) / 2f);

        if (featured.Count > 0)
        {
            DrawPortraitRow(
                new Rect(rect.x, y, rect.width, roleHeight),
                featured,
                0,
                featured.Count,
                EpitaphColonistCard.RolePortraitSize,
                RoleSlotGap,
                compact: false);
            y += roleHeight + gap;
        }

        if (small.Count > 0)
        {
            DrawPortraitRow(
                new Rect(rect.x, y, rect.width, smallHeight),
                small,
                0,
                small.Count,
                EpitaphColonistCard.SmallPortraitSize,
                SmallSlotGap,
                compact: true);
            y += smallHeight + moreGap;
        }

        if (showSeeAll)
        {
            DrawSeeMore(new Rect(rect.x, y, rect.width, SeeMoreHeight), remaining);
        }
    }

    internal static void SplitPortraitGroups(
        GameOverSnapshot snapshot,
        List<FallenColonist> featuredOnScreen,
        List<FallenColonist> smallOnScreen,
        List<FallenColonist> remaining)
    {
        List<FallenColonist> featured = new List<FallenColonist>();
        List<FallenColonist> others = new List<FallenColonist>();
        for (int i = 0; i < snapshot.fallen.Count; i++)
        {
            FallenColonist fallen = snapshot.fallen[i];
            if (fallen.Featured)
            {
                featured.Add(fallen);
            }
            else
            {
                others.Add(fallen);
            }
        }

        int featuredShown = Math.Min(featured.Count, MaxRolePortraits);
        for (int i = 0; i < featuredShown; i++)
        {
            featuredOnScreen.Add(featured[i]);
        }

        for (int i = featuredShown; i < featured.Count; i++)
        {
            remaining.Add(featured[i]);
        }

        int smallShown = Math.Min(others.Count, MaxSmallPortraits);
        for (int i = 0; i < smallShown; i++)
        {
            smallOnScreen.Add(others[i]);
        }

        for (int i = smallShown; i < others.Count; i++)
        {
            remaining.Add(others[i]);
        }
    }

    private void DrawPortraitRow(
        Rect rect,
        List<FallenColonist> colonists,
        int start,
        int count,
        float portraitSize,
        float gap,
        bool compact)
    {
        float slotWidth = portraitSize + gap;
        float totalWidth = count * slotWidth - gap;
        float x = rect.x + Math.Max(0f, (rect.width - totalWidth) / 2f);
        float slotHeight = EpitaphColonistCard.SlotHeight(portraitSize, compact);
        for (int i = 0; i < count; i++)
        {
            Rect slot = new Rect(x, rect.y, portraitSize, slotHeight);
            EpitaphColonistCard.Draw(
                slot,
                colonists[start + i],
                ContentAlpha,
                PortraitsInteractive,
                portraitSize,
                compact,
                snapshot.fallen);
            x += slotWidth;
        }
    }

    private void DrawSeeMore(Rect rect, List<FallenColonist> remaining)
    {
        Rect button = new Rect(
            rect.x + Math.Max(0f, (rect.width - SeeMoreWidth) / 2f),
            rect.y,
            SeeMoreWidth,
            SeeMoreHeight);
        bool clickable = FadeFinished;
        bool hover = clickable && Mouse.IsOver(button);

        Color fill = hover
            ? new Color(0.16f, 0.14f, 0.11f, 0.92f)
            : new Color(0.10f, 0.09f, 0.08f, 0.88f);
        fill.a *= ContentAlpha;
        Color edge = hover ? TitleColor : PanelEdge;
        Widgets.DrawBoxSolidWithOutline(button, fill, WithAlpha(edge, ContentAlpha), 1);

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        Color old = GUI.color;
        GUI.color = WithAlpha(TitleColor, ContentAlpha);
        Widgets.Label(button, "Epitaph_SeeRemaining".Translate(remaining.Count));
        GUI.color = old;
        Text.Anchor = TextAnchor.UpperLeft;

        if (clickable && Widgets.ButtonInvisible(button))
        {
            Find.WindowStack.Add(new Dialog_EpitaphRoster(remaining, snapshot.fallen));
        }
    }

    private void DrawButtons(Rect rect)
    {
        List<(string label, bool disabled, string? disabledReason, Action? action)> buttons = BuildButtons();
        if (buttons.Count == 0)
        {
            return;
        }

        bool clickable = FadeFinished;
        Color old = GUI.color;
        GUI.color = WithAlpha(Color.white, ContentAlpha);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        float width = (rect.width - ButtonGap * (buttons.Count - 1)) / buttons.Count;
        for (int i = 0; i < buttons.Count; i++)
        {
            (string label, bool disabled, string? disabledReason, Action? action) = buttons[i];
            Rect buttonRect = new Rect(rect.x + i * (width + ButtonGap), rect.y, width, rect.height);
            if (disabled && !disabledReason.NullOrEmpty())
            {
                TooltipHandler.TipRegion(buttonRect, disabledReason);
            }

            if (Widgets.ButtonText(buttonRect, label, active: clickable && !disabled))
            {
                Action? invoke = action;
                GUI.color = old;
                Close();
                invoke?.Invoke();
                return;
            }
        }

        GUI.color = old;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    private List<(string label, bool disabled, string? disabledReason, Action? action)> BuildButtons()
    {
        return GameOverButtons.Build(letter, snapshot.gameOverTick);
    }
}
