using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyEpitaph;

public class Dialog_EpitaphColonistDetail : Window
{
    private const float PortraitSize = 128f;
    private const float HeaderGap = 14f;
    private const float SectionGap = 14f;
    private const float SectionHeaderHeight = 26f;
    private const float LineHeight = 22f;
    private const float SkillRowHeight = 27f;
    private const float ColumnGap = 24f;
    private const float LabelWidth = 96f;
    private const float ButtonHeight = 34f;

    private static readonly Color TitleColor = new Color(0.92f, 0.86f, 0.74f);
    private static readonly Color MutedColor = new Color(0.72f, 0.68f, 0.62f);
    private static readonly Color PanelEdge = new Color(0.32f, 0.28f, 0.22f, 0.9f);
    private static readonly Color SkillBarBack = new Color(0.16f, 0.15f, 0.13f, 0.85f);
    private static readonly Color SkillBarFill = new Color(0.45f, 0.42f, 0.30f, 0.9f);
    private static readonly Color LinkColor = new Color(0.74f, 0.82f, 0.92f);
    private static readonly Color DisabledSkillColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);

    private readonly List<FallenColonist>? roster;
    private EpitaphColonistDetail detail;
    private Vector2 scroll;

    public override Vector2 InitialSize => new Vector2(900f, 760f);

    public Dialog_EpitaphColonistDetail(FallenColonist fallen, List<FallenColonist>? roster)
    {
        this.roster = roster;
        detail = EpitaphColonistDetail.Build(fallen, roster);
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

    public static bool AnyOpen => Find.WindowStack != null && Find.WindowStack.IsOpen<Dialog_EpitaphColonistDetail>();

    public override void DoWindowContents(Rect inRect)
    {
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Color oldColor = GUI.color;
        bool oldWrap = Text.WordWrap;

        try
        {
            Rect header = new Rect(inRect.x, inRect.y, inRect.width, PortraitSize);
            DrawHeader(header);

            float y = header.yMax + HeaderGap;
            Widgets.DrawLineHorizontal(inRect.x, y, inRect.width, PanelEdge);
            y += 12f;

            float bottom = ButtonHeight + 10f;
            Rect body = new Rect(inRect.x, y, inRect.width, inRect.height - y - bottom);
            DrawBody(body);

            Rect buttons = new Rect(inRect.x, inRect.yMax - ButtonHeight, inRect.width, ButtonHeight);
            DrawButtons(buttons);
        }
        finally
        {
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
            Text.WordWrap = oldWrap;
        }
    }

    private void DrawHeader(Rect rect)
    {
        Rect portrait = new Rect(rect.x, rect.y, PortraitSize, PortraitSize);
        EpitaphColonistCard.DrawPortraitOnly(portrait, detail.fallen, PortraitSize);

        Rect text = new Rect(portrait.xMax + 16f, rect.y, rect.width - PortraitSize - 16f, rect.height);
        float y = text.y;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        float nameHeight = Math.Max(32f, Text.CalcHeight(detail.fullName, text.width));
        Widgets.Label(new Rect(text.x, y, text.width, nameHeight), detail.fullName);
        y += nameHeight + 2f;

        Text.Font = GameFont.Small;
        y = DrawHeaderLine(text, y, detail.ageLine, MutedColor);
        y = DrawHeaderLine(text, y, detail.honorLine, detail.fallen.HonorBorder);
        y = DrawHeaderLine(text, y, detail.timeInColonyLine, TitleColor);
        DrawHeaderLine(text, y, detail.fallen.dead ? detail.deathLine : detail.statusLine, MutedColor);
    }

    private static float DrawHeaderLine(Rect text, float y, string line, Color color)
    {
        if (line.NullOrEmpty() || y > text.yMax - LineHeight)
        {
            return y;
        }

        GUI.color = color;
        Widgets.Label(new Rect(text.x, y, text.width, LineHeight), line);
        return y + LineHeight;
    }

    private void DrawBody(Rect view)
    {
        float width = view.width - 20f;
        float columnWidth = (width - ColumnGap) / 2f;
        float rightX = columnWidth + ColumnGap;

        float leftHeight = MeasureLeftColumn(columnWidth);
        float rightHeight = MeasureSkills();
        float columnsHeight = Math.Max(leftHeight, rightHeight);
        float deedsHeight = detail.deeds.Count > 0 ? SectionHeaderHeight + DeedRows() * LineHeight : 0f;
        float total = columnsHeight + (deedsHeight > 0f ? SectionGap + deedsHeight : 0f);

        Rect contents = new Rect(0f, 0f, width, Math.Max(total, view.height));
        Widgets.BeginScrollView(view, ref scroll, contents);
        try
        {
            DrawLeftColumn(0f, columnWidth);
            DrawSkills(rightX, columnWidth);
            if (deedsHeight > 0f)
            {
                DrawDeeds(0f, width, columnsHeight + SectionGap);
            }
        }
        finally
        {
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }

    private float MeasureLeftColumn(float width)
    {
        float height = 0f;
        if (detail.HasBackstory)
        {
            height += SectionHeaderHeight;
            if (!detail.childhood.NullOrEmpty())
            {
                height += LineHeight;
            }

            if (!detail.adulthood.NullOrEmpty())
            {
                height += LineHeight;
            }

            height += SectionGap;
        }

        if (!detail.traitLine.NullOrEmpty())
        {
            Text.Font = GameFont.Small;
            height += SectionHeaderHeight + Text.CalcHeight(detail.traitLine, width) + SectionGap;
        }

        if (detail.relations.Count > 0)
        {
            height += SectionHeaderHeight + detail.relations.Count * LineHeight;
        }

        return height;
    }

    private float MeasureSkills()
    {
        return detail.skills.Count > 0 ? SectionHeaderHeight + detail.skills.Count * SkillRowHeight : 0f;
    }

    private int DeedRows() => Mathf.CeilToInt(detail.deeds.Count / 2f);

    private void DrawLeftColumn(float x, float width)
    {
        float y = 0f;
        if (detail.HasBackstory)
        {
            y = DrawSectionHeader(x, y, width, "Epitaph_SectionBackstory".Translate());
            y = DrawPairLine(x, y, width, "Epitaph_DetailChildhood".Translate(), detail.childhood);
            y = DrawPairLine(x, y, width, "Epitaph_DetailAdulthood".Translate(), detail.adulthood);
            y += SectionGap;
        }

        if (!detail.traitLine.NullOrEmpty())
        {
            y = DrawSectionHeader(x, y, width, "Epitaph_SectionTraits".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            float traitHeight = Text.CalcHeight(detail.traitLine, width);
            Widgets.Label(new Rect(x, y, width, traitHeight), detail.traitLine);
            y += traitHeight + SectionGap;
        }

        if (detail.relations.Count > 0)
        {
            y = DrawSectionHeader(x, y, width, "Epitaph_SectionRelations".Translate());
            DrawRelations(x, y, width);
        }
    }

    private static float DrawSectionHeader(float x, float y, float width, string title)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.LowerLeft;
        GUI.color = TitleColor;
        Widgets.Label(new Rect(x, y, width, SectionHeaderHeight), title);
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        return y + SectionHeaderHeight;
    }

    private static float DrawPairLine(float x, float y, float width, string label, string value)
    {
        if (value.NullOrEmpty())
        {
            return y;
        }

        Text.Font = GameFont.Small;
        GUI.color = MutedColor;
        Widgets.Label(new Rect(x, y, LabelWidth, LineHeight), label);
        GUI.color = Color.white;
        Widgets.Label(new Rect(x + LabelWidth, y, width - LabelWidth, LineHeight), value);
        return y + LineHeight;
    }

    private void DrawSkills(float x, float width)
    {
        if (detail.skills.Count == 0)
        {
            return;
        }

        float y = DrawSectionHeader(x, 0f, width, "Epitaph_SectionSkills".Translate());
        Text.Font = GameFont.Small;
        for (int i = 0; i < detail.skills.Count; i++)
        {
            EpitaphSkill skill = detail.skills[i];
            Rect bar = new Rect(x, y + i * SkillRowHeight, width, SkillRowHeight - 4f);
            Widgets.DrawBoxSolid(bar, SkillBarBack);
            if (!skill.disabled && skill.level > 0)
            {
                float fraction = Mathf.Clamp01(skill.level / 20f);
                Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), SkillBarFill);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = skill.disabled ? DisabledSkillColor : Color.white;
            Widgets.Label(new Rect(bar.x + 8f, bar.y, bar.width - 74f, bar.height), skill.label);

            if (!skill.disabled)
            {
                DrawPassion(new Rect(bar.xMax - 58f, bar.y + 3f, 18f, bar.height - 6f), skill.passion);
            }

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = skill.disabled ? DisabledSkillColor : Color.white;
            Widgets.Label(new Rect(bar.xMax - 36f, bar.y, 28f, bar.height), skill.disabled ? "-" : skill.level.ToString());
        }

        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }

    private static void DrawPassion(Rect rect, Passion passion)
    {
        Texture2D? icon = passion switch
        {
            Passion.Minor => SkillUI.PassionMinorIcon,
            Passion.Major => SkillUI.PassionMajorIcon,
            _ => null,
        };

        if (icon == null)
        {
            return;
        }

        GUI.color = Color.white;
        Widgets.DrawTextureFitted(rect, icon, 1f);
    }

    private void DrawRelations(float x, float y, float width)
    {
        Text.Font = GameFont.Small;
        for (int i = 0; i < detail.relations.Count; i++)
        {
            EpitaphRelation relation = detail.relations[i];
            Rect row = new Rect(x, y + i * LineHeight, width, LineHeight);

            GUI.color = MutedColor;
            Widgets.Label(new Rect(row.x, row.y, LabelWidth, row.height), relation.label);

            Rect nameRect = new Rect(row.x + LabelWidth, row.y, row.width - LabelWidth, row.height);
            bool clickable = relation.entry != null;
            GUI.color = clickable ? LinkColor : Color.white;
            Widgets.Label(nameRect, relation.name);
            if (clickable)
            {
                Widgets.DrawHighlightIfMouseover(nameRect);
                if (Widgets.ButtonInvisible(nameRect))
                {
                    detail = EpitaphColonistDetail.Build(relation.entry!, roster);
                    scroll = Vector2.zero;
                }
            }
        }

        GUI.color = Color.white;
    }

    private void DrawDeeds(float x, float width, float y)
    {
        y = DrawSectionHeader(x, y, width, "Epitaph_SectionDeeds".Translate());
        float columnWidth = (width - ColumnGap) / 2f;
        Text.Font = GameFont.Small;
        for (int i = 0; i < detail.deeds.Count; i++)
        {
            EpitaphDeed deed = detail.deeds[i];
            int column = i % 2;
            int row = i / 2;
            Rect rowRect = new Rect(x + column * (columnWidth + ColumnGap), y + row * LineHeight, columnWidth, LineHeight);

            GUI.color = MutedColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, rowRect.width - 72f, rowRect.height), deed.label);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rowRect.xMax - 72f, rowRect.y, 72f, rowRect.height), deed.value);
        }

        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }

    private void DrawButtons(Rect rect)
    {
        Text.Font = GameFont.Small;
        if (Widgets.ButtonText(new Rect(rect.center.x - 80f, rect.y, 160f, rect.height), "Epitaph_DetailClose".Translate()))
        {
            Close();
        }
    }
}
