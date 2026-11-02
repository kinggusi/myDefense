using UnityEngine;
using UnityEngine.UI;

/// <summary>Responsive four-column collection; never changes the Battle board.</summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class LobbyCollectionGrid : MonoBehaviour
{
    public const int Columns = 4;
    public const int SidePadding = 36;
    private GridLayoutGroup grid;

    private void OnEnable() => RefreshLayout();
    private void OnRectTransformDimensionsChange() => RefreshLayout();

    public void RefreshLayout()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        RectTransform rect = transform as RectTransform;
        if (grid == null || rect == null || rect.rect.width <= 0f) return;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        if (grid.padding.left != SidePadding || grid.padding.right != SidePadding)
            grid.padding = new RectOffset(SidePadding, SidePadding, grid.padding.top, grid.padding.bottom);
        float width = Mathf.Max(1f, (rect.rect.width - grid.padding.horizontal
                                      - grid.spacing.x * (Columns - 1)) / Columns);
        Vector2 cell = new Vector2(width, width * 1.42f);
        if ((grid.cellSize - cell).sqrMagnitude > 0.01f) grid.cellSize = cell;
    }
}
