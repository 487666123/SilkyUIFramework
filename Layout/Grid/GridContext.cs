namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 单次布局计算的上下文，集中保存父容器、子项区域和轨道状态。
/// </summary>
public sealed class GridContext(UIElementGroup parent)
{
    /// <summary> 当前执行 Grid 布局的父容器。 </summary>
    public UIElementGroup Parent { get; } = parent;

    /// <summary> 当前参与布局的流内子项及其已计算出的 Grid 区域。 </summary>
    public GridLayoutItem[] Items { get; private set; } = [];

    /// <summary> 行轨道状态。显式轨道来自 TemplateRows，隐式轨道使用 Auto。 </summary>
    public GridTrackState[] Rows { get; private set; } = [];

    /// <summary> 列轨道状态。显式轨道来自 TemplateColumns，隐式轨道使用 Auto。 </summary>
    public GridTrackState[] Columns { get; private set; } = [];

    public float TotalRowsHeight => GridLayoutHelper.SumTracks(Rows, Parent.Gap.Height);

    public float TotalColumnsWidth => GridLayoutHelper.SumTracks(Columns, Parent.Gap.Width);

    /// <summary>
    /// 重建本轮布局的子项和显式轨道。
    /// 每次布局都会从父容器当前属性重新生成，避免上一帧的隐式轨道污染本帧。
    /// </summary>
    public void Initialize()
    {
        Items = [.. Parent.InFlowChildren
            .Select(element => new GridLayoutItem(element, CreateInitialArea(element)))];

        Rows = CreateTrackStates(Parent.TemplateRows);
        Columns = CreateTrackStates(Parent.TemplateColumns);
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
            var rows = new GridTrackState[rowCount];
            Array.Copy(Rows, rows, Rows.Length);
            for (var i = Rows.Length; i < rows.Length; i++)
            {
                rows[i] = new GridTrackState(GridTrack.Auto);
            }

            Rows = rows;
        }

        // 列数不足时创建隐式列。显式列保持原定义，新增列统一按 Auto 轨道处理。
        if (Columns.Length < columnCount)
        {
            var columns = new GridTrackState[columnCount];
            Array.Copy(Columns, columns, Columns.Length);
            for (var i = Columns.Length; i < columns.Length; i++)
            {
                columns[i] = new GridTrackState(GridTrack.Auto);
            }

            Columns = columns;
        }
    }

    /// <summary>
    /// 获取一个 Grid 区域覆盖的总宽度，包含区域内部的列间距。
    /// </summary>
    public float GetAreaWidth(GridArea area)
    {
        return GridLayoutHelper.SumTracks(Columns, area.Column, area.ColumnSpan, Parent.Gap.Width);
    }

    /// <summary>
    /// 获取一个 Grid 区域覆盖的总高度，包含区域内部的行间距。
    /// </summary>
    public float GetAreaHeight(GridArea area)
    {
        return GridLayoutHelper.SumTracks(Rows, area.Row, area.RowSpan, Parent.Gap.Height);
    }

    private static GridTrackState[] CreateTrackStates(IReadOnlyList<GridTrack> tracks)
    {
        if (tracks.Count == 0) return [new GridTrackState(GridTrack.Auto)];

        var states = new GridTrackState[tracks.Count];
        for (var i = 0; i < tracks.Count; i++)
        {
            states[i] = new GridTrackState(tracks[i]);
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
