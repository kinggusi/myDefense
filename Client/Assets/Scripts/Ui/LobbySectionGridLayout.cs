using UnityEngine;
using UnityEngine.UI;

/// <summary>Four-column cards with a full-width, short locked-Mythic section header.</summary>
public sealed class LobbySectionGridLayout : GridLayoutGroup
{
    public const string LockedHeaderName = "LockedMythicHeader";
    public const float HeaderHeight = 76f;
    public const float SectionGap = 30f;

    public override void CalculateLayoutInputVertical()
    {
        float height = Arrange(-1);
        SetLayoutInputForAxis(height, height, -1f, 1);
    }

    public override void SetLayoutHorizontal() => Arrange(0);
    public override void SetLayoutVertical() => Arrange(1);

    private float Arrange(int axis)
    {
        int column = 0;
        float y = padding.top;
        foreach (RectTransform child in rectChildren)
        {
            if (child.name == LockedHeaderName)
            {
                // Finish even a partial owned row before starting the locked section.
                if (column > 0) { y += cellSize.y + spacing.y; column = 0; }
                y += SectionGap;
                if (axis == 0) SetChildAlongAxis(child, 0, padding.left,
                    Mathf.Max(1f, rectTransform.rect.width - padding.horizontal));
                if (axis == 1) SetChildAlongAxis(child, 1, y, HeaderHeight);
                y += HeaderHeight + spacing.y;
                continue;
            }
            if (axis == 0) SetChildAlongAxis(child, 0,
                padding.left + column * (cellSize.x + spacing.x), cellSize.x);
            if (axis == 1) SetChildAlongAxis(child, 1, y, cellSize.y);
            column++;
            if (column == LobbyCollectionGrid.Columns)
            {
                column = 0;
                y += cellSize.y + spacing.y;
            }
        }
        if (column > 0) y += cellSize.y + spacing.y;
        return y + padding.bottom - (rectChildren.Count > 0 ? spacing.y : 0f);
    }
}
