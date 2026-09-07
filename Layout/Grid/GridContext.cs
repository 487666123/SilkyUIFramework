namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 单次布局计算的上下文，集中保存父容器、子项区域和轨道状态。
/// </summary>
public sealed class GridContext(UIElementGroup container)
{
    public UIElementGroup Container { get; } = container;

    public GridItem[] Items { get; private set; } = [];

    public GridTrackOutput[] Rows { get; private set; } = [];
    public GridTrackOutput[] Columns { get; private set; } = [];

    public float TotalRowsHeight => GridLayoutHelper.SumTracks(Rows, Container.Gap.Height);

    public float TotalColumnsWidth => GridLayoutHelper.SumTracks(Columns, Container.Gap.Width);

    /// <summary>
    /// 重建本轮布局的子项和显式轨道。
    /// 每次布局都会从父容器当前属性重新生成，避免上一帧的隐式轨道污染本帧。
    /// </summary>
    public void Initialize()
    {
        Items = [.. Container.InFlowChildren
            .Select(element => new GridItem(element, CreateInitialArea(element)))];

        Rows = CreateTrackStates(Container.TemplateRows);
        Columns = CreateTrackStates(Container.TemplateColumns);
    }

    /// <summary>
    /// 放置完成后一次性确保行列轨道数量足够容纳最终区域。
    /// </summary>
    public void EnsureTrackCapacity(int rowCount, int columnCount)
    {
        // Grid 至少需要保留 1 行 1 列，避免后续自动放置和轨道求和面对空数组。
        rowCount = Math.Max(1, rowCount);
        columnCount = Math.Max(1, columnCount);

        // 行数不足时创建隐式行。显式行保持原定义，新增行统一按 Auto 轨道处理。
        if (Rows.Length < rowCount)
        {
            var rows = new GridTrackOutput[rowCount];
            Array.Copy(Rows, rows, Rows.Length);
            for (var i = Rows.Length; i < rows.Length; i++)
            {
                rows[i] = new GridTrackOutput(GridTrack.Auto);
            }

            Rows = rows;
        }

        // 列数不足时创建隐式列。显式列保持原定义，新增列统一按 Auto 轨道处理。
        if (Columns.Length < columnCount)
        {
            var columns = new GridTrackOutput[columnCount];
            Array.Copy(Columns, columns, Columns.Length);
            for (var i = Columns.Length; i < columns.Length; i++)
            {
                columns[i] = new GridTrackOutput(GridTrack.Auto);
            }

            Columns = columns;
        }
    }

    /// <summary>
    /// 获取一个 Grid 区域覆盖的总宽度，包含区域内部的列间距。
    /// </summary>
    public float GetAreaWidth(GridArea area)
    {
        return GridLayoutHelper.SumTracks(Columns, area.Column, area.ColumnSpan, Container.Gap.Width);
    }

    /// <summary>
    /// 获取一个 Grid 区域覆盖的总高度，包含区域内部的行间距。
    /// </summary>
    public float GetAreaHeight(GridArea area)
    {
        return GridLayoutHelper.SumTracks(Rows, area.Row, area.RowSpan, Container.Gap.Height);
    }

    private static GridTrackOutput[] CreateTrackStates(IReadOnlyList<GridTrack> tracks)
    {
        if (tracks.Count == 0) return [new GridTrackOutput(GridTrack.Auto)];

        var states = new GridTrackOutput[tracks.Count];
        for (var i = 0; i < tracks.Count; i++)
        {
            states[i] = new GridTrackOutput(tracks[i]);
        }

        return states;
    }

    private static GridArea CreateInitialArea(UIView element)
    {
        return new GridArea(
            element.RowSpan.Start ?? 0,
            element.ColumnSpan.Start ?? 0,
            element.RowSpan.Size,
            element.ColumnSpan.Size);
    }
}
