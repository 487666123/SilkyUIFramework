namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 轨道尺寸解析器，按单轴分别计算行高和列宽。
/// </summary>
public static class GridTrackSizing
{
    /// <summary>
    /// 解析列轨道尺寸。非 FitWidth 时，列百分比和 fr 基于父容器 InnerBounds.Width。
    /// </summary>
    public static void ResolveColumns(GridContext context)
    {
        ResolveTracks(
            context.Columns,
            context.Container.FitWidth,
            context.Container.InnerBounds.Width,
            context.Container.Gap.Width,
            context.Items,
            isColumnAxis: true);
    }

    /// <summary>
    /// 解析行轨道尺寸。非 FitHeight 时，行百分比和 fr 基于父容器 InnerBounds.Height。
    /// </summary>
    public static void ResolveRows(GridContext context)
    {
        ResolveTracks(
            context.Rows,
            context.Container.FitHeight,
            context.Container.InnerBounds.Height,
            context.Container.Gap.Height,
            context.Items,
            isColumnAxis: false);
    }

    /// <summary>
    /// 在轨道最终尺寸确定后，计算行列偏移。
    /// </summary>
    public static void UpdateOffsets(GridContext context)
    {
        GridLayoutHelper.UpdateOffsets(
            context.Columns,
            context.Container.Gap.Width,
            context.Container.InnerBounds.Width,
            context.Container.FitWidth
                ? GridContentAlignment.Start
                : context.Container.GridContentHorizontalAlignment);
        GridLayoutHelper.UpdateOffsets(
            context.Rows,
            context.Container.Gap.Height,
            context.Container.InnerBounds.Height,
            context.Container.FitHeight
                ? GridContentAlignment.Start
                : context.Container.GridContentVerticalAlignment);
    }

    /// <summary>
    /// 单轴轨道尺寸计算入口。
    /// 顺序为：重置缓存 -> 固定/百分比轨道 -> 内容撑开的轨道 -> 有限上限轨道增长 -> fr 轨道 -> 非负修正。
    /// </summary>
    private static void ResolveTracks(
        GridTrackOutput[] tracks,
        bool fitAxis,
        float availableSize,
        float gap,
        IReadOnlyList<GridItem> items,
        bool isColumnAxis)
    {
        if (tracks.Length == 0) return;

        ResetTracks(tracks);
        ResolveFixedTracks(tracks, fitAxis, availableSize);
        ResolveAutoTracks(tracks, items, isColumnAxis, gap);
        GrowFixedLimitTracks(tracks, fitAxis, availableSize, gap);
        ResolveFractionTracks(tracks, fitAxis, availableSize, gap);
        ClampNegativeTracks(tracks);
    }

    /// <summary>
    /// 清空上一轮计算得到的 Size，保留轨道定义和 Offset。
    /// </summary>
    private static void ResetTracks(GridTrackOutput[] tracks)
    {
        for (var i = 0; i < tracks.Length; i++)
        {
            tracks[i].Size = 0f;
            tracks[i].MinSize = 0f;
            tracks[i].MaxSize = float.PositiveInfinity;
        }
    }

    /// <summary>
    /// 解析轨道的最小和最大边界，并以最小边界初始化轨道尺寸。
    /// Fit 轴上的百分比端点失效：最小值按 0、最大值不设限；含 Auto 的轨道仍可按内容撑开。
    /// </summary>
    private static void ResolveFixedTracks(GridTrackOutput[] tracks, bool fitAxis, float availableSize)
    {
        for (var i = 0; i < tracks.Length; i++)
        {
            var definition = tracks[i].Definition;
            var minSize = ResolveBound(definition.Min, fitAxis, availableSize, isMin: true);
            var maxSize = ResolveBound(definition.Max, fitAxis, availableSize, isMin: false);

            tracks[i].MinSize = minSize;
            tracks[i].MaxSize = Math.Max(minSize, maxSize);
            tracks[i].Size = minSize;
        }
    }

    private static float ResolveBound(
        GridTrackSize definition,
        bool fitAxis,
        float availableSize,
        bool isMin)
    {
        return definition.TemplateType switch
        {
            TemplateType.Pixels => Math.Max(0f, definition.Value),
            TemplateType.Percent => fitAxis
                ? isMin ? 0f : float.PositiveInfinity
                : Math.Max(0f, availableSize * definition.Value),
            TemplateType.Auto or TemplateType.Fraction =>
                isMin ? 0f : float.PositiveInfinity,
            _ => 0f
        };
    }

    /// <summary>
    /// 根据子项本轮 OuterBounds 解析内容相关轨道。
    /// 单轨道需求先合并为基础尺寸；跨轨道需求独立计算后逐轨道取最大值，避免子项顺序影响结果。
    /// </summary>
    private static void ResolveAutoTracks(
        GridTrackOutput[] tracks,
        IReadOnlyList<GridItem> items,
        bool isColumnAxis,
        float gap)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            var area = item.Area;
            var start = isColumnAxis ? area.Column : area.Row;
            var span = isColumnAxis ? area.ColumnSpan : area.RowSpan;
            if (span != 1 || start >= tracks.Length) continue;

            var outerSize = isColumnAxis ? item.Element.OuterBounds.Width : item.Element.OuterBounds.Height;
            GrowContentSizedTrack(ref tracks[start], outerSize);
        }

        // 所有跨轨道子项读取同一份基础尺寸，不读取其他跨轨道子项刚刚增加的尺寸。
        var baseSizes = new float[tracks.Length];
        for (var i = 0; i < tracks.Length; i++)
        {
            baseSizes[i] = tracks[i].Size;
        }

        foreach (var item in items)
        {
            var area = item.Area;
            var start = isColumnAxis ? area.Column : area.Row;
            var span = isColumnAxis ? area.ColumnSpan : area.RowSpan;
            if (span <= 1 || start >= tracks.Length) continue;

            var outerSize = isColumnAxis ? item.Element.OuterBounds.Width : item.Element.OuterBounds.Height;
            var end = Math.Min(tracks.Length, start + span);
            var candidateSizes = (float[])baseSizes.Clone();
            DistributeSpanningAutoSize(tracks, candidateSizes, start, end, outerSize, gap);

            for (var i = start; i < end; i++)
            {
                tracks[i].Size = Math.Max(tracks[i].Size, candidateSizes[i]);
            }
        }
    }

    /// <summary> 让含 Auto 的轨道按内容增长，但不突破本轮有效的上限。 </summary>
    private static void GrowContentSizedTrack(ref GridTrackOutput track, float size)
    {
        if (!CanGrowByContent(track)) return;

        track.Size = Math.Min(track.MaxSize, Math.Max(track.Size, size));
    }

    /// <summary>
    /// 基础尺寸加内部 gap 已经足够时不增长；否则先扣除 gap 和不可增长轨道的尺寸，
    /// 剩余需求除以 Auto 轨道数量得到统一目标，仅提升低于目标的轨道。
    /// 若实际总尺寸仍不足，固定达到上限的轨道，重新求其余轨道的统一目标。
    /// </summary>
    private static void DistributeSpanningAutoSize(
        GridTrackOutput[] tracks,
        float[] candidateSizes,
        int start,
        int end,
        float outerSize,
        float gap)
    {
        var targetSize = Math.Max(0f, outerSize - Math.Max(0, end - start - 1) * gap);
        var baseSize = 0f;
        var remainingSize = targetSize;
        var growingTracks = new List<int>(end - start);

        for (var i = start; i < end; i++)
        {
            baseSize += candidateSizes[i];
            if (CanGrowByContent(tracks[i]))
            {
                growingTracks.Add(i);
            }
            else
            {
                // 固定轨道等不可被内容撑开的轨道只计入已有尺寸，不参与分配。
                remainingSize -= candidateSizes[i];
            }
        }

        if (targetSize <= baseSize) return;

        while (growingTracks.Count > 0)
        {
            var target = Math.Max(0f, remainingSize) / growingTracks.Count;
            foreach (var index in growingTracks)
            {
                candidateSizes[index] = Math.Min(tracks[index].MaxSize, Math.Max(candidateSizes[index], target));
            }

            var currentSize = 0f;
            for (var i = start; i < end; i++) currentSize += candidateSizes[i];
            if (currentSize >= targetSize) break;

            // 只在区域实际放不下时补偿；到顶的轨道退出后，从需求中扣除它实际承担的尺寸。
            var reachedLimit = false;
            for (var i = growingTracks.Count - 1; i >= 0; i--)
            {
                var index = growingTracks[i];
                if (candidateSizes[index] < tracks[index].MaxSize) continue;

                remainingSize -= candidateSizes[index];
                growingTracks.RemoveAt(i);
                reachedLimit = true;
            }

            if (!reachedLimit) break;
        }
    }

    /// <summary>
    /// 仅显式含 Auto 的轨道允许被内容撑开；纯百分比和纯 fr 在 Fit 轴上也不例外。
    /// </summary>
    private static bool CanGrowByContent(GridTrackOutput track) =>
        track.Definition.Min.TemplateType is TemplateType.Auto ||
        track.Definition.Max.TemplateType is TemplateType.Auto;

    /// <summary>
    /// 在确定尺寸的轴上，将剩余空间平均分配给尚未达到固定/百分比上限的轨道。
    /// 达到上限的轨道退出分配，剩余空间继续分给其他轨道；fr 轨道暂时保留基础尺寸。
    /// </summary>
    private static void GrowFixedLimitTracks(GridTrackOutput[] tracks, bool fitAxis, float availableSize, float gap)
    {
        if (fitAxis) return;

        var remaining = availableSize - GridLayoutHelper.SumTracks(tracks, gap);
        if (remaining <= 0f) return;

        var growingTracks = new List<int>(tracks.Length);
        for (var i = 0; i < tracks.Length; i++)
        {
            if (tracks[i].Definition.Max.TemplateType is TemplateType.Pixels or TemplateType.Percent &&
                tracks[i].Size < tracks[i].MaxSize)
            {
                growingTracks.Add(i);
            }
        }

        while (growingTracks.Count > 0 && remaining > 0f)
        {
            var share = remaining / growingTracks.Count;
            var reachedLimit = false;
            for (var i = growingTracks.Count - 1; i >= 0; i--)
            {
                var index = growingTracks[i];
                var capacity = tracks[index].MaxSize - tracks[index].Size;
                if (capacity > share) continue;

                tracks[index].Size = tracks[index].MaxSize;
                remaining -= capacity;
                growingTracks.RemoveAt(i);
                reachedLimit = true;
            }

            // 先固定触及上限的轨道，再重算其他轨道的份额，避免丢失被上限截断的空间。
            if (reachedLimit) continue;

            foreach (var index in growingTracks)
            {
                tracks[index].Size += share;
            }
            break;
        }
    }

    /// <summary>
    /// 在确定尺寸的轴上求统一的 fr 单位；份额小于基础尺寸的轨道固定后，重新计算其余份额。
    /// fr 总和不足 1 时保留未分配空间。Fit 轴不分配 fr 空间，只保留明确下限或显式 Auto 的内容尺寸。
    /// </summary>
    private static void ResolveFractionTracks(GridTrackOutput[] tracks, bool fitAxis, float availableSize, float gap)
    {
        if (fitAxis) return;

        var remaining = availableSize - Math.Max(0, tracks.Length - 1) * gap;
        var flexibleTracks = new List<int>(tracks.Length);
        for (var i = 0; i < tracks.Length; i++)
        {
            if (tracks[i].Definition.Max.TemplateType is TemplateType.Fraction &&
                tracks[i].Definition.Max.Value > 0f)
            {
                flexibleTracks.Add(i);
            }
            else
            {
                // 非 fr 轨道及 0fr 轨道保留已有尺寸。
                remaining -= tracks[i].Size;
            }
        }

        while (flexibleTracks.Count > 0)
        {
            var totalFraction = 0f;
            foreach (var index in flexibleTracks)
            {
                totalFraction += tracks[index].Definition.Max.Value;
            }

            var fractionSize = Math.Max(0f, remaining) / Math.Max(1f, totalFraction);
            var frozeTrack = false;
            for (var i = flexibleTracks.Count - 1; i >= 0; i--)
            {
                var index = flexibleTracks[i];
                var size = tracks[index].Definition.Max.Value * fractionSize;
                if (size >= tracks[index].Size) continue;

                // 基础尺寸包含最小值和已测得的内容贡献，不能因 fr 分配而缩小。
                remaining -= tracks[index].Size;
                flexibleTracks.RemoveAt(i);
                frozeTrack = true;
            }

            if (frozeTrack) continue;

            foreach (var index in flexibleTracks)
            {
                tracks[index].Size = tracks[index].Definition.Max.Value * fractionSize;
            }
            break;
        }
    }

    /// <summary>
    /// 对外部输入和中间计算结果做兜底修正，避免负尺寸进入布局结果。
    /// </summary>
    private static void ClampNegativeTracks(GridTrackOutput[] tracks)
    {
        for (var i = 0; i < tracks.Length; i++)
        {
            tracks[i].MinSize = Math.Max(0f, tracks[i].MinSize);
            tracks[i].MaxSize = Math.Max(tracks[i].MinSize, tracks[i].MaxSize);
            tracks[i].Size = Math.Clamp(tracks[i].Size, tracks[i].MinSize, tracks[i].MaxSize);
        }
    }
}
