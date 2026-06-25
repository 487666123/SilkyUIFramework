namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 子项放置算法。该实现使用已放置矩形列表和 sparse cursor，不使用二维 cell 占用表。
/// </summary>
public static class GridPlacement
{
    /// <summary>
    /// 按 CSS Grid 自动放置的基本优先级处理子项：
    /// 先放置行列都明确的项，再放置只明确一个轴的项，最后放置完全自动的项。
    /// </summary>
    public static void PlaceItems(GridContext context)
    {
        var flow = GridFlow.From(context.Parent.GridFlowDirection);
        var placedItems = new List<PlacedGridItem>(context.Items.Length);
        var rowCount = Math.Max(1, context.Rows.Length);
        var columnCount = Math.Max(1, context.Columns.Length);

        PlaceDefiniteItems(context, flow, placedItems, ref rowCount, ref columnCount);
        PlaceSemiDefiniteItems(context, flow, placedItems, ref rowCount, ref columnCount);
        PlaceAutoItems(context, flow, placedItems, ref rowCount, ref columnCount);

        context.EnsureTrackCapacity(rowCount, columnCount);
    }

    /// <summary>
    /// 放置行列起点都明确的子项。它们优先占位，后续自动项会绕开这些矩形区域。
    /// </summary>
    private static void PlaceDefiniteItems(
        GridContext context,
        GridFlow flow,
        List<PlacedGridItem> placedItems,
        ref int rowCount,
        ref int columnCount)
    {
        for (var i = 0; i < context.Items.Length; i++)
        {
            var element = context.Items[i].Element;
            if (!element.RowSpan.Start.HasValue || !element.ColumnSpan.Start.HasValue) continue;

            var area = new GridArea(
                element.RowSpan.Start.Value,
                element.ColumnSpan.Start.Value,
                element.RowSpan.Size,
                element.ColumnSpan.Size);

            PlaceItem(context, flow, placedItems, i, area, ref rowCount, ref columnCount);
        }
    }

    /// <summary>
    /// 放置只明确一条轴的子项。自动轴允许扩展，保持当前工程实现的宽松行为。
    /// </summary>
    private static void PlaceSemiDefiniteItems(
        GridContext context,
        GridFlow flow,
        List<PlacedGridItem> placedItems,
        ref int rowCount,
        ref int columnCount)
    {
        for (var i = 0; i < context.Items.Length; i++)
        {
            var element = context.Items[i].Element;
            var hasRow = element.RowSpan.Start.HasValue;
            var hasColumn = element.ColumnSpan.Start.HasValue;
            if (hasRow == hasColumn) continue;

            var area = hasRow
                ? FindInFixedRow(
                    flow,
                    element.RowSpan.Start!.Value,
                    element.RowSpan.Size,
                    element.ColumnSpan.Size,
                    placedItems)
                : FindInFixedColumn(
                    flow,
                    element.ColumnSpan.Start!.Value,
                    element.RowSpan.Size,
                    element.ColumnSpan.Size,
                    placedItems);

            PlaceItem(context, flow, placedItems, i, area, ref rowCount, ref columnCount);
        }
    }

    /// <summary>
    /// 放置没有明确行列起点的子项。cursor 按 GridFlowDirection 指定方向执行 sparse 扫描。
    /// </summary>
    private static void PlaceAutoItems(
        GridContext context,
        GridFlow flow,
        List<PlacedGridItem> placedItems,
        ref int rowCount,
        ref int columnCount)
    {
        var cursorMajor = 0;
        var cursorMinor = 0;
        var minorLimit = GetInitialMinorLimit(context, flow);

        for (var i = 0; i < context.Items.Length; i++)
        {
            var element = context.Items[i].Element;
            if (element.RowSpan.Start.HasValue || element.ColumnSpan.Start.HasValue) continue;

            var majorSpan = GetMajorSpan(flow, element.RowSpan.Size, element.ColumnSpan.Size);
            var minorSpan = GetMinorSpan(flow, element.RowSpan.Size, element.ColumnSpan.Size);
            minorLimit = Math.Max(minorLimit, minorSpan);

            var rect = FindAuto(placedItems, majorSpan, minorSpan, minorLimit, ref cursorMajor, ref cursorMinor);
            var area = ToGridArea(flow, rect);

            PlaceItem(context, flow, placedItems, i, area, ref rowCount, ref columnCount);

            cursorMajor = rect.MajorStart;
            cursorMinor = rect.MinorEnd;
            NormalizeCursor(ref cursorMajor, ref cursorMinor, minorLimit);
        }
    }

    private static GridArea FindInFixedRow(
        GridFlow flow,
        int row,
        int rowSpan,
        int columnSpan,
        IReadOnlyList<PlacedGridItem> placedItems)
    {
        row = Math.Max(0, row);

        for (var column = 0; ;)
        {
            var area = new GridArea(row, column, rowSpan, columnSpan);
            var candidate = ToFlowRect(flow, area);
            var conflict = FindConflict(placedItems, candidate);
            if (!conflict.HasValue) return area;

            column = Math.Max(column + 1, conflict.Value.Area.ColumnEnd);
        }
    }

    private static GridArea FindInFixedColumn(
        GridFlow flow,
        int column,
        int rowSpan,
        int columnSpan,
        IReadOnlyList<PlacedGridItem> placedItems)
    {
        column = Math.Max(0, column);

        for (var row = 0; ;)
        {
            var area = new GridArea(row, column, rowSpan, columnSpan);
            var candidate = ToFlowRect(flow, area);
            var conflict = FindConflict(placedItems, candidate);
            if (!conflict.HasValue) return area;

            row = Math.Max(row + 1, conflict.Value.Area.RowEnd);
        }
    }

    private static FlowRect FindAuto(
        IReadOnlyList<PlacedGridItem> placedItems,
        int majorSpan,
        int minorSpan,
        int minorLimit,
        ref int cursorMajor,
        ref int cursorMinor)
    {
        for (; ; )
        {
            if (cursorMinor + minorSpan > minorLimit)
            {
                cursorMajor++;
                cursorMinor = 0;
                continue;
            }

            var candidate = new FlowRect(
                cursorMajor,
                cursorMinor,
                cursorMajor + majorSpan,
                cursorMinor + minorSpan);

            var conflict = FindConflict(placedItems, candidate);
            if (!conflict.HasValue) return candidate;

            cursorMinor = Math.Max(cursorMinor + 1, conflict.Value.Rect.MinorEnd);
        }
    }

    private static void PlaceItem(
        GridContext context,
        GridFlow flow,
        List<PlacedGridItem> placedItems,
        int itemIndex,
        GridArea area,
        ref int rowCount,
        ref int columnCount)
    {
        context.Items[itemIndex].Area = area;
        rowCount = Math.Max(rowCount, area.RowEnd);
        columnCount = Math.Max(columnCount, area.ColumnEnd);

        var placedItem = new PlacedGridItem(itemIndex, area, ToFlowRect(flow, area));
        placedItems.Add(placedItem);
    }

    private static PlacedGridItem? FindConflict(IReadOnlyList<PlacedGridItem> placedItems, FlowRect candidate)
    {
        PlacedGridItem? conflict = null;

        foreach (var item in placedItems)
        {
            if (!GridLayoutHelper.Overlaps(candidate, item.Rect)) continue;

            if (!conflict.HasValue ||
                item.Rect.MinorEnd < conflict.Value.Rect.MinorEnd ||
                item.Rect.MinorEnd == conflict.Value.Rect.MinorEnd &&
                item.Rect.MajorEnd < conflict.Value.Rect.MajorEnd)
            {
                conflict = item;
            }
        }

        return conflict;
    }

    private static int GetInitialMinorLimit(GridContext context, GridFlow flow)
    {
        var limit = flow.Minor == GridFlowDirection.Column ? context.Columns.Length : context.Rows.Length;

        foreach (var item in context.Items)
        {
            var element = item.Element;
            if (flow.Minor == GridFlowDirection.Column)
            {
                limit = Math.Max(limit, element.ColumnSpan.Size);
                if (element.ColumnSpan.Start is { } start)
                {
                    limit = Math.Max(limit, start + element.ColumnSpan.Size);
                }
            }
            else
            {
                limit = Math.Max(limit, element.RowSpan.Size);
                if (element.RowSpan.Start is { } start)
                {
                    limit = Math.Max(limit, start + element.RowSpan.Size);
                }
            }
        }

        return Math.Max(1, limit);
    }

    private static FlowRect ToFlowRect(GridFlow flow, GridArea area)
    {
        return flow.Major == GridFlowDirection.Row
            ? new FlowRect(area.Row, area.Column, area.RowEnd, area.ColumnEnd)
            : new FlowRect(area.Column, area.Row, area.ColumnEnd, area.RowEnd);
    }

    private static GridArea ToGridArea(GridFlow flow, FlowRect rect)
    {
        return flow.Major == GridFlowDirection.Row
            ? new GridArea(
                rect.MajorStart,
                rect.MinorStart,
                rect.MajorEnd - rect.MajorStart,
                rect.MinorEnd - rect.MinorStart)
            : new GridArea(
                rect.MinorStart,
                rect.MajorStart,
                rect.MinorEnd - rect.MinorStart,
                rect.MajorEnd - rect.MajorStart);
    }

    private static int GetMajorSpan(GridFlow flow, int rowSpan, int columnSpan)
    {
        return flow.Major == GridFlowDirection.Row ? rowSpan : columnSpan;
    }

    private static int GetMinorSpan(GridFlow flow, int rowSpan, int columnSpan)
    {
        return flow.Minor == GridFlowDirection.Row ? rowSpan : columnSpan;
    }

    private static void NormalizeCursor(ref int cursorMajor, ref int cursorMinor, int minorLimit)
    {
        if (cursorMinor < minorLimit) return;

        cursorMajor++;
        cursorMinor = 0;
    }
}
