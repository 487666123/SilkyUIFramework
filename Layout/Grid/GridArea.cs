namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 子项最终占据的矩形区域，使用 0-based 轨道索引和跨度描述。
/// </summary>
public readonly struct GridArea(int row, int column, int rowSpan, int columnSpan)
{
    /// <summary> 起始行索引。 </summary>
    public int Row { get; } = Math.Max(0, row);

    /// <summary> 起始列索引。 </summary>
    public int Column { get; } = Math.Max(0, column);

    /// <summary> 覆盖的行数，最小为 1。 </summary>
    public int RowSpan { get; } = Math.Max(1, rowSpan);

    /// <summary> 覆盖的列数，最小为 1。 </summary>
    public int ColumnSpan { get; } = Math.Max(1, columnSpan);

    /// <summary> 结束行索引，开区间。 </summary>
    public int RowEnd => Row + RowSpan;

    /// <summary> 结束列索引，开区间。 </summary>
    public int ColumnEnd => Column + ColumnSpan;
}
