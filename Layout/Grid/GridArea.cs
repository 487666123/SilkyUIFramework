namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 子项最终占据的矩形区域
/// </summary>
public readonly struct GridArea(int row, int column, int rowSpan, int columnSpan)
{
    public int Row { get; } = Math.Max(0, row);
    public int Column { get; } = Math.Max(0, column);
    public int RowSpan { get; } = Math.Max(1, rowSpan);
    public int ColumnSpan { get; } = Math.Max(1, columnSpan);

    public int RowEnd => Row + RowSpan;
    public int ColumnEnd => Column + ColumnSpan;
}
