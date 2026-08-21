using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyEpitaph;

public class Dialog_Epitaph : Window
{
    private const float PanelWidth = 860f;
    private const float PanelHeight = 640f;
    private const float PortraitSize = 92f;
    private const float ButtonHeight = 38f;
    private const float ButtonGap = 8f;

    private static readonly AccessTools.FieldRef<DiaOption, string> DiaOptionText =
        AccessTools.FieldRefAccess<DiaOption, string>("text");

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.07f, 0.94f);
    private static readonly Color PanelEdge = new Color(0.32f, 0.28f, 0.22f, 0.9f);
    private static readonly Color TitleColor = new Color(0.92f, 0.86f, 0.74f);
    private static readonly Color MutedColor = new Color(0.72f, 0.68f, 0.62f);

    private readonly GameOverSnapshot snapshot;
    private ChoiceLetter? letter;

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
        Widgets.DrawBoxSolid(inRect, OverlayColor);

        Rect panel = new Rect(
            (inRect.width - PanelWidth) / 2f,
            (inRect.height - PanelHeight) / 2f,
            PanelWidth,
            PanelHeight);
        Widgets.DrawBoxSolidWithOutline(panel, PanelColor, PanelEdge, 2);
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
            GUI.color = TitleColor;
            Rect headingRect = new Rect(inner.x, y, inner.width, 32f);
            Widgets.Label(headingRect, snapshot.heading);
            y += 36f;

            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Rect nameRect = new Rect(inner.x, y, inner.width, Text.CalcHeight(snapshot.colonyName, inner.width));
            Widgets.Label(nameRect, snapshot.colonyName);
            y += nameRect.height + 10f;

            Text.Font = GameFont.Small;
            GUI.color = MutedColor;
            if (!snapshot.subtitle.NullOrEmpty())
            {
                float subHeight = Text.CalcHeight(snapshot.subtitle, inner.width);
                Rect subRect = new Rect(inner.x, y, inner.width, subHeight);
                Widgets.Label(subRect, snapshot.subtitle);
                y += subHeight + 8f;
            }

            GUI.color = TitleColor;
            Rect lastedRect = new Rect(inner.x, y, inner.width, 24f);
            Widgets.Label(lastedRect, snapshot.lastedText);
            y += 36f;

            Widgets.DrawLineHorizontal(inner.x + 80f, y, inner.width - 160f, PanelEdge);
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
            GUI.color = MutedColor;
            Widgets.Label(rect, "Epitaph_NoColonists".Translate());
            return;
        }

        int shown = Math.Min(snapshot.fallen.Count, GameOverSnapshot.MaxPortraits);
        int overflow = snapshot.fallen.Count - shown;
        float slotWidth = PortraitSize + 16f;
        float totalWidth = shown * slotWidth;
        if (overflow > 0)
        {
            totalWidth += 90f;
        }

        float x = rect.x + Math.Max(0f, (rect.width - totalWidth) / 2f);
        float portraitY = rect.y + 8f;

        for (int i = 0; i < shown; i++)
        {
            FallenColonist fallen = snapshot.fallen[i];
            Rect slot = new Rect(x, portraitY, PortraitSize, rect.height - 16f);
            DrawFallen(slot, fallen);
            x += slotWidth;
        }

        if (overflow > 0)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = MutedColor;
            Rect overflowRect = new Rect(x, portraitY + PortraitSize / 2f - 12f, 120f, 24f);
            Widgets.Label(overflowRect, "Epitaph_AndMore".Translate(overflow));
        }
    }

    private static void DrawFallen(Rect slot, FallenColonist fallen)
    {
        Rect portraitRect = new Rect(slot.x, slot.y, PortraitSize, PortraitSize);
        Widgets.DrawBoxSolid(portraitRect, new Color(0f, 0f, 0f, 0.45f));
        Widgets.DrawBox(portraitRect);

        bool drewPortrait = false;
        if (fallen.pawn != null)
        {
            try
            {
                RenderTexture portrait = PortraitsCache.Get(
                    fallen.pawn,
                    new Vector2(PortraitSize, PortraitSize),
                    Rot4.South,
                    cameraZoom: 1.15f,
                    healthStateOverride: PawnHealthState.Mobile);
                if (portrait != null)
                {
                    Widgets.DrawTextureFitted(portraitRect, portrait, 1f);
                    drewPortrait = true;
                }
            }
            catch (Exception)
            {
                drewPortrait = false;
            }
        }

        if (!drewPortrait)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = MutedColor;
            Widgets.Label(portraitRect, "Epitaph_PortraitFailed".Translate());
        }

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperCenter;
        GUI.color = Color.white;
        Rect nameRect = new Rect(slot.x - 6f, portraitRect.yMax + 4f, PortraitSize + 12f, 22f);
        Widgets.Label(nameRect, fallen.name);

        GUI.color = MutedColor;
        Rect statusRect = new Rect(slot.x - 10f, nameRect.yMax, PortraitSize + 20f, 36f);
        Text.WordWrap = true;
        Widgets.Label(statusRect, fallen.status);
    }

    private void DrawButtons(Rect rect)
    {
        List<(string label, bool disabled, string? disabledReason, Action? action)> buttons = BuildButtons();
        if (buttons.Count == 0)
        {
            return;
        }

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

            if (Widgets.ButtonText(buttonRect, label, active: !disabled) && action != null)
            {
                Action invoke = action;
                Close();
                invoke();
            }
        }
    }

    private List<(string label, bool disabled, string? disabledReason, Action? action)> BuildButtons()
    {
        List<(string, bool, string?, Action?)> buttons = new List<(string, bool, string?, Action?)>();
        if (letter != null)
        {
            try
            {
                foreach (DiaOption option in letter.Choices)
                {
                    if (option == null)
                    {
                        continue;
                    }

                    string? label = DiaOptionText(option);
                    if (label.NullOrEmpty())
                    {
                        continue;
                    }

                    buttons.Add((label, option.disabled, option.disabledReason, option.action));
                }

                if (buttons.Count > 0)
                {
                    return buttons;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Colony Epitaph] Could not read vanilla game-over choices; using built-in buttons.\n" + e);
                buttons.Clear();
            }
        }

        buttons.Add(("Epitaph_KeepWatching".Translate(), false, null, () => { }));
        AddWanderersButton(buttons);
        if (!snapshot.permadeath)
        {
            buttons.Add(("Epitaph_LoadSave".Translate(), false, null, () => Find.WindowStack.Add(new Dialog_SaveFileList_Load())));
        }

        buttons.Add(("Epitaph_MainMenu".Translate(), false, null, GenScene.GoToMainMenu));
        return buttons;
    }

    private void AddWanderersButton(List<(string, bool, string?, Action?)> buttons)
    {
        bool canSpawn = false;
        try
        {
            canSpawn = Find.GameEnder != null && Find.GameEnder.CanSpawnNewWanderers();
        }
        catch (Exception)
        {
            canSpawn = false;
        }

        string? reason = null;
        if (!canSpawn)
        {
            int waited = Find.TickManager.TicksGame - snapshot.gameOverTick;
            int remaining = GameEnder.NewWanderersDelay - waited;
            if (remaining > 0 && Find.GameEnder != null && Find.GameEnder.gameEnding)
            {
                reason = "Epitaph_WanderersWait".Translate(GenDate.ToStringTicksToPeriod(remaining, allowSeconds: false));
            }
            else
            {
                reason = "Epitaph_WanderersExhausted".Translate();
            }
        }

        buttons.Add((
            "Epitaph_CreateWanderers".Translate(),
            !canSpawn,
            reason,
            () => Find.WindowStack.Add(new Dialog_ChooseNewWanderers())));
    }
}
