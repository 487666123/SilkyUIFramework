namespace SilkyUIFramework.Layout.Grid;

internal readonly struct PlacedGridItem(int itemIndex, GridArea area, FlowRect rect)
{
    public int ItemIndex { get; } = itemIndex;

    public GridArea Area { get; } = area;

    public FlowRect Rect { get; } = rect;
}
