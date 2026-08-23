using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyEpitaph;

internal static class EpitaphColonistCard
{
    public const float PortraitSize = 92f;
    public const float RolePortraitSize = 88f;
    public const float SmallPortraitSize = 56f;

    private static readonly Color DefaultBorder = new Color(0.72f, 0.68f, 0.62f);
    private static readonly Color DeceasedBorder = new Color(0.42f, 0.43f, 0.46f);
    private static readonly Color MutedColor = new Color(0.72f, 0.68f, 0.62f);
    private static readonly Color LivingWell = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color DeceasedWell = new Color(0.07f, 0.08f, 0.10f, 0.96f);
    private static readonly Color DeceasedTint = new Color(0.36f, 0.38f, 0.42f);
    private static readonly Color DeceasedVeil = new Color(0.04f, 0.05f, 0.07f, 0.38f);
    private static readonly Color DeceasedName = new Color(0.58f, 0.58f, 0.60f);

    public static float SlotHeight(float portraitSize, bool compact)
    {
        return compact ? portraitSize + 24f : portraitSize + 64f;
    }

    public static void Draw(Rect slot, FallenColonist fallen, float alpha, bool interactive, List<FallenColonist>? roster)
    {
        Draw(slot, fallen, alpha, interactive, PortraitSize, compact: false, roster);
    }

    public static void DrawPortraitOnly(Rect portraitRect, FallenColonist fallen, float portraitSize)
    {
        DrawPortrait(portraitRect, fallen, 1f, portraitSize, compact: false);
    }

    public static void Draw(
        Rect slot,
        FallenColonist fallen,
        float alpha,
        bool interactive,
        float portraitSize,
        bool compact,
        List<FallenColonist>? roster)
    {
        Rect portraitRect = new Rect(slot.x, slot.y, portraitSize, portraitSize);
        Color old = GUI.color;
        DrawPortrait(portraitRect, fallen, alpha, portraitSize, compact);

        if (interactive && fallen.pawn != null && Mouse.IsOver(portraitRect))
        {
            Widgets.DrawHighlight(portraitRect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Find.WindowStack.Add(new Dialog_EpitaphColonistDetail(fallen, roster));
                Event.current.Use();
            }
        }

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperCenter;
        Text.WordWrap = true;
        GUI.color = WithAlpha(fallen.dead ? DeceasedName : Color.white, alpha);
        Rect nameRect = new Rect(slot.x - 4f, portraitRect.yMax + 3f, portraitSize + 8f, 18f);
        Widgets.Label(nameRect, fallen.name);

        if (!compact && !fallen.HonorLabel.NullOrEmpty())
        {
            Color honor = fallen.dead
                ? Color.Lerp(fallen.HonorBorder, DeceasedBorder, 0.55f)
                : fallen.HonorBorder;
            GUI.color = WithAlpha(honor, alpha);
            Rect roleRect = new Rect(slot.x - 6f, nameRect.yMax, portraitSize + 12f, 36f);
            Widgets.Label(roleRect, fallen.HonorLabel);
        }

        GUI.color = old;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static void DrawPortrait(Rect portraitRect, FallenColonist fallen, float alpha, float portraitSize, bool compact)
    {
        Color well = fallen.dead ? DeceasedWell : LivingWell;
        well.a *= alpha;
        Widgets.DrawBoxSolid(portraitRect, well);

        Color old = GUI.color;
        GUI.color = WithAlpha(PortraitBorder(fallen), alpha);
        Widgets.DrawBox(portraitRect, fallen.dead || fallen.Featured ? 2 : 1);
        GUI.color = old;

        bool drewPortrait = false;
        if (fallen.pawn != null)
        {
            try
            {
                RenderTexture portrait = PortraitsCache.Get(
                    fallen.pawn,
                    new Vector2(portraitSize, portraitSize),
                    Rot4.South,
                    cameraZoom: compact ? 1.05f : 1.15f,
                    healthStateOverride: PawnHealthState.Mobile);
                if (portrait != null)
                {
                    if (fallen.dead)
                    {
                        GUI.color = WithAlpha(DeceasedTint, alpha);
                        Widgets.DrawTextureFitted(portraitRect, portrait, 1f);
                        Widgets.DrawBoxSolid(portraitRect, WithAlpha(DeceasedVeil, alpha));
                    }
                    else
                    {
                        Widgets.DrawTextureFitted(portraitRect, portrait, 1f, alpha);
                    }

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
            GUI.color = WithAlpha(MutedColor, alpha);
            Widgets.Label(portraitRect, "Epitaph_PortraitFailed".Translate());
        }

        GUI.color = old;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static Color PortraitBorder(FallenColonist fallen)
    {
        if (fallen.dead)
        {
            return fallen.Featured
                ? Color.Lerp(fallen.HonorBorder, DeceasedBorder, 0.7f)
                : DeceasedBorder;
        }

        return fallen.Featured ? fallen.HonorBorder : DefaultBorder;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }
}
