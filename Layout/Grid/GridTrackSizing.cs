namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 轨道尺寸解析器，按单轴分别计算行高和列宽。
/// </summary>
public static class GridTrackSizing
{
    /// <summary>
    /// 解析列轨道尺寸。列百分比和 fr 基于父容器 InnerBounds.Width。
    /// </summary>
    public static void ResolveColumns(GridContext context)
    {
        ResolveTracks(
            context.Columns,
            context.Parent.FitWidth,
            context.Parent.InnerBounds.Width,
            context.Parent.Gap.Width,
            context.Items,
            isColumnAxis: true);
    }

    /// <summary>
    /// 解析行轨道尺寸。行百分比和 fr 基于父容器 InnerBounds.Height。
    /// </summary>
    public static void ResolveRows(GridContext context)
    {
        ResolveTracks(
            context.Rows,
            context.Parent.FitHeight,
            context.Parent.InnerBounds.Height,
            context.Parent.Gap.Height,
            context.Items,
            isColumnAxis: false);
    }

    /// <summary>
    /// 在轨道最终尺寸确定后，计算行列偏移。
    /// </summary>
    public static void UpdateOffsets(GridContext context)
    {
        GridLayoutHelper.UpdateOffsets(context.Columns, context.Parent.Gap.Width);
        GridLayoutHelper.UpdateOffsets(context.Rows, context.Parent.Gap.Height);
    }

    /// <summary>
    /// 单轴轨道尺寸计算入口。
    /// 顺序为：重置缓存 -> 固定/百分比轨道 -> 内容撑开的轨道 -> fr 轨道 -> 非负修正。
    /// </summary>
    private static void ResolveTracks(
        GridTrackState[] tracks,
        bool fitAxis,
        float availableSize,
        float gap,
        IReadOnlyList<GridLayoutItem> items,
        bool isColumnAxis)
    {
        if (tracks.Length == 0) return;

        ResetTracks(tracks);
        ResolveFixedTracks(tracks, fitAxis, availableSize);
        ResolveAutoTracks(tracks, items, isColumnAxis, fitAxis, gap);
        ResolveFractionTracks(tracks, fitAxis, availableSize, gap);
        ClampNegativeTracks(tracks);
    }

    /// <summary>
    /// 清空上一轮计算得到的 BaseSize/FinalSize，保留轨道定义和 Offset。
    /// </summary>
    private static void ResetTracks(GridTrackState[] tracks)
    {
        for (var i = 0; i < tracks.Length; i++)
        {
            tracks[i].BaseSize = 0f;
            tracks[i].FinalSize = 0f;
        }
    }

    /// <summary>
    /// 先解析无需依赖内容的轨道。
    /// 当容器对应轴是 Fit 时，百分比没有明确参照尺寸，暂时按 0 处理，后续可由内容撑开。
    /// </summary>
    private static void ResolveFixedTracks(GridTrackState[] tracks, bool fitAxis, float availableSize)
    {
        for (var i = 0; i < tracks.Length; i++)
        {
            var definition = tracks[i].Definition;
            switch (definition.TemplateType)
            {
                case TemplateType.Pixels:
                    tracks[i].BaseSize = Math.Max(0f, definition.Value);
                    tracks[i].FinalSize = tracks[i].BaseSize;
                    break;
                case TemplateType.Percent:
                    tracks[i].BaseSize = fitAxis ? 0f : Math.Max(0f, availableSize * definition.Value);
                    tracks[i].FinalSize = tracks[i].BaseSize;
                    break;
            }
        }
    }

    /// <summary>
    /// 根据子项当前 OuterBounds 解析内容相关轨道。
    /// 第一版只做工程化近似：单轨道子项直接撑开轨道，跨轨道子项把不足尺寸平均分配给可被内容撑开的轨道。
    /// </summary>
    private static void ResolveAutoTracks(
        GridTrackState[] tracks,
        IReadOnlyList<GridLayoutItem> items,
        bool isColumnAxis,
        bool fitAxis,
        float gap)
    {
        foreach (var item in items)
        {
            var area = item.Area;
            var start = isColumnAxis ? area.Column : area.Row;
            var span = isColumnAxis ? area.ColumnSpan : area.RowSpan;
            if (span <= 0 || start >= tracks.Length) continue;

            var outerSize = isColumnAxis ? item.Element.OuterBounds.Width : item.Element.OuterBounds.Height;
            if (span == 1)
            {
                GrowContentSizedTrack(ref tracks[start], outerSize, fitAxis);
                continue;
            }

            DistributeSpanningAutoSize(tracks, start, span, outerSize, fitAxis, gap);
        }
    }

    /// <summary>
    /// 让单个可内容撑开的轨道至少达到指定尺寸。
    /// </summary>
    private static void GrowContentSizedTrack(ref GridTrackState track, float size, bool fitAxis)
    {
        if (!CanGrowByContent(track.Definition, fitAxis)) return;

        track.BaseSize = Math.Max(track.BaseSize, size);
        track.FinalSize = Math.Max(track.FinalSize, track.BaseSize);
    }

    /// <summary>
    /// 处理跨多条轨道的子项。
    /// 子项尺寸先扣除区域内部 gap，再把不足部分平均分摊给 Auto 轨道；
    /// 如果容器对应轴是 Fit，Percent/Fr 轨道也允许按内容撑开。
    /// </summary>
    private static void DistributeSpanningAutoSize(
        GridTrackState[] tracks,
        int start,
        int span,
        float outerSize,
        bool fitAxis,
        float gap)
    {
        var end = Math.Min(tracks.Length, start + span);
        var growableCount = 0;
        var currentSize = 0f;
        var targetSize = Math.Max(0f, outerSize - Math.Max(0, span - 1) * gap);

        for (var i = start; i < end; i++)
        {
            currentSize += tracks[i].FinalSize;
            if (CanGrowByContent(tracks[i].Definition, fitAxis))
            {
                growableCount++;
            }
        }

        if (growableCount <= 0 || targetSize <= currentSize) return;

        var share = (targetSize - currentSize) / growableCount;
        for (var i = start; i < end; i++)
        {
            if (!CanGrowByContent(tracks[i].Definition, fitAxis)) continue;

            tracks[i].BaseSize += share;
            tracks[i].FinalSize = Math.Max(tracks[i].FinalSize, tracks[i].BaseSize);
        }
    }

    /// <summary>
    /// 判断轨道是否允许被内容撑开。
    /// Auto 始终允许；Percent/Fr 只有在 Fit 轴上没有明确可用空间时才允许。
    /// </summary>
    private static bool CanGrowByContent(GridTrack definition, bool fitAxis)
    {
        if (definition.TemplateType is TemplateType.Auto) return true;

        return fitAxis && definition.TemplateType is TemplateType.Percent or TemplateType.Fraction;
    }

    /// <summary>
    /// 将剩余空间分配给 fr 轨道。
    /// 如果容器对应轴是 Fit，则没有 definite free space，fr 轨道保留内容撑开的尺寸。
    /// </summary>
    private static void ResolveFractionTracks(GridTrackState[] tracks, bool fitAxis, float availableSize, float gap)
    {
        var totalFraction = 0f;
        var usedSize = Math.Max(0, tracks.Length - 1) * gap;

        for (var i = 0; i < tracks.Length; i++)
        {
            usedSize += tracks[i].FinalSize;
            if (tracks[i].Definition.TemplateType is TemplateType.Fraction)
            {
                totalFraction += Math.Max(0f, tracks[i].Definition.Value);
            }
        }

        if (totalFraction <= 0f) return;

        if (fitAxis)
        {
            for (var i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].Definition.TemplateType is TemplateType.Fraction)
                {
                    tracks[i].FinalSize = Math.Max(tracks[i].FinalSize, tracks[i].BaseSize);
                }
            }

            return;
        }

        var remaining = Math.Max(0f, availableSize - usedSize);
        for (var i = 0; i < tracks.Length; i++)
        {
            if (tracks[i].Definition.TemplateType is not TemplateType.Fraction) continue;

            var fraction = Math.Max(0f, tracks[i].Definition.Value);
            tracks[i].FinalSize = remaining * fraction / totalFraction;
        }
    }

    /// <summary>
    /// 对外部输入和中间计算结果做兜底修正，避免负尺寸进入布局结果。
    /// </summary>
    private static void ClampNegativeTracks(GridTrackState[] tracks)
    {
        for (var i = 0; i < tracks.Length; i++)
        {
            tracks[i].BaseSize = Math.Max(0f, tracks[i].BaseSize);
            tracks[i].FinalSize = Math.Max(0f, tracks[i].FinalSize);
        }
    }
}
