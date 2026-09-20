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
    /// 解析行轨道尺寸。高度不由内容决定时（含比例高度），行百分比和 fr 基于父容器 InnerBounds.Height。
    /// </summary>
    public static void ResolveRows(GridContext context)
    {
        ResolveTracks(
            context.Rows,
            context.Container.FitHeightToContent,
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
            context.Container.FitHeightToContent
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
    /// 单轨道需求先合并为基础尺寸；非 flexible 跨项按跨度分组，flexible 跨项最后处理，组内合并增量。
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
            var minimumOuterSize = isColumnAxis
                ? item.Element.WidthMertrics.MinOuter
                : item.Element.HeightMertrics.MinOuter;
            GrowContentSizedTrack(ref tracks[start], outerSize, minimumOuterSize);
        }

        var spanningItems = new List<SpanningItem>();
        foreach (var item in items)
        {
            var area = item.Area;
            var start = isColumnAxis ? area.Column : area.Row;
            var span = isColumnAxis ? area.ColumnSpan : area.RowSpan;
            if (span <= 1 || start >= tracks.Length) continue;

            var end = Math.Min(tracks.Length, start + span);
            var crossesFlexible = false;
            for (var i = start; i < end; i++)
                crossesFlexible |= tracks[i].Definition.Max.TemplateType is TemplateType.Fraction;

            spanningItems.Add(new SpanningItem(start, end,
                isColumnAxis ? item.Element.OuterBounds.Width : item.Element.OuterBounds.Height,
                isColumnAxis ? item.Element.WidthMertrics.MinOuter : item.Element.HeightMertrics.MinOuter,
                crossesFlexible));
        }

        // 非 flexible 跨项按跨度递增处理。同一组先独立求增量再逐轨道取最大值，避免元素顺序影响结果。
        foreach (var group in spanningItems.Where(item => !item.CrossesFlexible)
                     .GroupBy(item => item.End - item.Start).OrderBy(group => group.Key))
        {
            ResolveSpanningGroup(tracks, group.ToArray(), gap, flexible: false);
        }

        // 跨 flexible 轨道的项单独作为一组，只向其中允许内容增长的 flexible 轨道分配。
        ResolveSpanningGroup(tracks, spanningItems.Where(item => item.CrossesFlexible).ToArray(), gap, flexible: true);
    }

    private readonly record struct SpanningItem(int Start, int End, float Size, float MinimumSize, bool CrossesFlexible);

    /// <summary> 单轨道子项的明确最小尺寸优先于 Auto 下限轨道的上限；其余内容增长仍受有效上限限制。 </summary>
    private static void GrowContentSizedTrack(ref GridTrackOutput track, float size, float minimumOuterSize)
    {
        if (!CanGrowByContent(track)) return;

        if (track.Definition.Min.TemplateType is TemplateType.Auto)
        {
            track.MinSize = Math.Max(track.MinSize, minimumOuterSize);
            track.Size = Math.Max(track.Size, track.MinSize);
            // Auto 下限解析后可能超过声明的 max，此时提高有效增长上限，而不是压低最小尺寸。
            track.MaxSize = Math.Max(track.MaxSize, track.Size);
        }

        track.Size = Math.Min(track.MaxSize, Math.Max(track.Size, size));
    }

    private static void ResolveSpanningGroup(GridTrackOutput[] tracks, SpanningItem[] items, float gap, bool flexible)
    {
        if (items.Length == 0) return;

        // 明确的子项最小尺寸先建立 Auto 下限；普通内容测量值再按有效上限增长。
        ResolveSpanningContributions(tracks, items, gap, flexible, minimum: true);
        ResolveSpanningContributions(tracks, items, gap, flexible, minimum: false);
    }

    private static void ResolveSpanningContributions(
        GridTrackOutput[] tracks, SpanningItem[] items, float gap, bool flexible, bool minimum)
    {
        var baseSizes = tracks.Select(track => track.Size).ToArray();
        var plannedSizes = (float[])baseSizes.Clone();
        foreach (var item in items)
        {
            var candidateSizes = (float[])baseSizes.Clone();
            var extra = (minimum ? item.MinimumSize : item.Size) - (item.End - item.Start - 1) * gap;
            var affected = new List<int>();
            for (var i = item.Start; i < item.End; i++)
            {
                extra -= baseSizes[i];
                if (flexible && tracks[i].Definition.Max.TemplateType is not TemplateType.Fraction) continue;
                if (minimum ? tracks[i].Definition.Min.TemplateType is TemplateType.Auto : CanGrowByContent(tracks[i]))
                    affected.Add(i);
            }

            if (extra <= 0f || affected.Count == 0) continue;

            // 分配缺少的尺寸，而不是把各轨道补到相同总宽度。
            var remaining = DistributeExtraSpace(tracks, candidateSizes, affected, extra, flexible, beyondLimits: false);
            if (minimum && remaining > 0f)
            {
                // 显式最小尺寸无法在增长上限内满足时，优先扩大具有内容最大值的轨道；否则扩大 Auto 下限轨道。
                var intrinsicMaximums = affected.Where(index => tracks[index].Definition.Max.TemplateType is TemplateType.Auto).ToList();
                DistributeExtraSpace(tracks, candidateSizes,
                    intrinsicMaximums.Count > 0 ? intrinsicMaximums : affected,
                    remaining, flexible, beyondLimits: true);
            }

            foreach (var index in affected)
                plannedSizes[index] = Math.Max(plannedSizes[index], candidateSizes[index]);
        }

        for (var i = 0; i < tracks.Length; i++)
        {
            if (plannedSizes[i] <= baseSizes[i]) continue;
            tracks[i].Size = plannedSizes[i];
            if (!minimum) continue;
            tracks[i].MinSize = Math.Max(tracks[i].MinSize, plannedSizes[i]);
            tracks[i].MaxSize = Math.Max(tracks[i].MaxSize, tracks[i].Size);
        }
    }

    /// <summary> 均分额外尺寸，或按 fr 权重分配；达到上限的轨道冻结后，其余轨道继续分配。 </summary>
    private static float DistributeExtraSpace(
        GridTrackOutput[] tracks, float[] sizes, List<int> affected, float extra, bool flexible, bool beyondLimits)
    {
        var growing = affected.Where(index => beyondLimits || sizes[index] < tracks[index].MaxSize).ToList();
        while (extra > 0f && growing.Count > 0)
        {
            var totalFraction = flexible ? growing.Sum(index => Math.Max(0f, tracks[index].Definition.Max.Value)) : 0f;
            // fr 总和不足 1 时，先按比例分配对应部分，再均分其余部分（CSS 跨 flexible 轨道规则）。
            var equalRemainder = flexible ? Math.Max(0f, 1f - totalFraction) / growing.Count : 1f;
            var totalWeight = flexible ? Math.Max(1f, totalFraction) : growing.Count;
            var frozen = false;
            var available = extra;
            for (var i = growing.Count - 1; i >= 0; i--)
            {
                var index = growing[i];
                var weight = flexible ? Math.Max(0f, tracks[index].Definition.Max.Value) + equalRemainder : 1f;
                var share = available * (weight / totalWeight);
                var capacity = beyondLimits ? float.PositiveInfinity : tracks[index].MaxSize - sizes[index];
                if (capacity > share) continue;

                sizes[index] = tracks[index].MaxSize;
                extra = Math.Max(0f, extra - capacity);
                growing.RemoveAt(i);
                frozen = true;
            }

            if (frozen) continue;
            foreach (var index in growing)
            {
                var weight = flexible ? Math.Max(0f, tracks[index].Definition.Max.Value) + equalRemainder : 1f;
                sizes[index] += extra * (weight / totalWeight);
            }
            return 0f;
        }
        return extra;
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
