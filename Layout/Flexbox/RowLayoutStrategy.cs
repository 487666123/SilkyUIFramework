namespace SilkyUIFramework.Layout.Flexbox;

/// <summary>
/// 行方向（Row）的 Flexbox 布局策略
/// </summary>
public class RowLayoutStrategy : IFlexboxLayoutStrategy
{
    public static IFlexboxLayoutStrategy Instance { get; } = new RowLayoutStrategy();

    /// <inheritdoc />
    public void MeasureChildren(FlexboxContext context)
    {
        if (context.Parent.FlexWrap && !context.Parent.FitWidth)
            WrapRow(context);
        else
            SingleRow(context);
    }

    /// <inheritdoc />
    public void Measure(FlexboxContext context)
    {
        MeasureSize(context, context.Parent.Gap.Width, out var mainSize, out var crossSize);
        if (context.Parent.FitWidth) context.Parent.SetInnerWidthClamped(mainSize);
        if (context.Parent.FitHeight) context.Parent.SetInnerHeightClamped(crossSize);
    }

    /// <inheritdoc />
    public void ResizeChildrenWidth(FlexboxContext context)
    {
        // 宽度可能被父元素拉伸, 再次计算元素换行
        if (context.Parent.FlexWrap)
            WrapRow(context);
        else
        {
            foreach (var line in context.Lines)
                line.UpdateMainSizeByRow(context.Parent.Gap.Width);
        }

        RowGrowOrShrink(context);
    }

    /// <inheritdoc />
    public void RecalculateHeight(FlexboxContext context)
    {
        if (!context.Parent.FitHeight) return;
        context.Parent.SetInnerHeightClamped(FlexboxHelper.CalculateCrossSize(context.Lines, context.Parent.Gap.Height));
    }

    /// <inheritdoc />
    public void RecalculateChildrenHeight(FlexboxContext context)
    {
        foreach (var line in context.Lines)
            line.CrossSize = line.MaxOuterHeight();
    }

    /// <inheritdoc />
    public void ResizeChildrenHeight(FlexboxContext context)
    {
        if (context.Parent.CrossContentAlignment == CrossContentAlignment.Stretch)
        {
            var remaining = context.Parent.InnerBounds.Height - FlexboxHelper.CalculateCrossSize(context.Lines, context.Parent.Gap.Height);
            if (remaining > 0)
            {
                var share = remaining / context.Lines.Count;
                foreach (var line in context.Lines)
                    line.CrossSize += share;
            }
        }

        if (context.Parent.CrossAlignment == CrossAlignment.Stretch)
        {
            foreach (var line in context.Lines)
            {
                foreach (var el in line.Elements.Where(el =>
                         el.Parent.FitHeight || !(el.OuterBounds.Height >= line.CrossSize)))
                {
                    el.SetOuterHeightClamped(line.CrossSize);
                }
            }
        }

        var innerBounds = context.Parent.InnerBounds;

        foreach (var line in context.Lines)
            line.UpdateMainAlignment(context.Parent.MainAlignment, innerBounds.Width, context.Parent.Gap.Width);

        FlexboxHelper.UpdateCrossContentAlignment(
            context.Lines,
            innerBounds.Height,
            context.Parent.Gap.Height,
            context.Parent.CrossContentAlignment,
            out var crossGapCache,
            out var crossOffsetCache);
        context.CrossGap = crossGapCache;
        context.CrossOffset = crossOffsetCache;
    }

    /// <inheritdoc />
    public void UpdateChildrenLayoutPosition(FlexboxContext context)
    {
        var crossStart = context.CrossOffset;

        foreach (var line in context.Lines)
        {
            var left = line.MainOffset;

            foreach (var el in line.Elements)
            {
                var crossOffset = FlexboxHelper.CalculateCrossOffset(line.CrossSize, el.OuterBounds.Height, context.Parent.CrossAlignment);
                el.SetLayoutOffset(left, crossStart + crossOffset);
                left += el.OuterBounds.Width + line.MainGap;
            }

            crossStart += line.CrossSize + context.CrossGap;
        }
    }

    #region 私有方法

    private static void SingleRow(FlexboxContext context)
    {
        context.ClearLines();
        context.AddLine(FlexLine.CreateSingleRow(context.Parent.InFlowChildren, context.Parent.Gap.Width));
    }

    private static void WrapRow(FlexboxContext context)
    {
        var width = context.Parent.InnerBounds.Width;
        var hGap = context.Parent.Gap.Width;
        var elements = context.Parent.InFlowChildren;
        context.ClearLines();

        if (elements.Count == 0) return;

        var line = FlexLine.CreateRow(elements[0]);
        context.AddLine(line);

        for (var i = 1; i < elements.Count; i++)
        {
            var element = elements[i];

            if (line.MainSize + element.OuterBounds.Width + hGap <= width)
            {
                line.AddByRow(element, hGap);
                continue;
            }

            line = FlexLine.CreateRow(element);
            context.AddLine(line);
        }
    }

    private static void MeasureSize(FlexboxContext context, float gap, out float mainSize, out float crossSize)
    {
        mainSize = 0f;
        crossSize = (context.Lines.Count - 1) * gap;

        foreach (var line in context.Lines)
        {
            mainSize = Math.Max(mainSize, line.MainSize);
            crossSize += line.CrossSize;
        }
    }

    private static void RowGrowOrShrink(FlexboxContext context)
    {
        var mainSize = context.Parent.InnerBounds.Width;
        var gap = context.Parent.Gap.Width;

        foreach (var line in context.Lines)
        {
            var remaining = mainSize - line.MainSize;
            switch (remaining)
            {
                case > 0:
                {
                    var growElements = line.Elements
                        .Where(el => el.FlexGrow > 0)
                        .Select(el => (Element: el, AvailableGrowth: el.WidthMertrics.MaxOuter - el.OuterBounds.Width))
                        .Where(item => item.AvailableGrowth > 0)
                        .OrderBy(item => item.AvailableGrowth).ToArray();
                    var totalGrow = growElements.Sum(el => el.Element.FlexGrow);

                    foreach (var (element, availableGrowth) in growElements)
                    {
                        var share = remaining / totalGrow;
                        var alloc = Math.Min(availableGrowth, share * element.FlexGrow);

                        element.SetOuterWidthClamped(element.OuterBounds.Width + alloc);

                        remaining -= alloc;
                        totalGrow -= element.FlexGrow;
                    }

                    break;
                }
                case < 0:
                {
                    var shrinkElements = line.Elements
                        .Where(el => el.FlexShrink > 0)
                        .Select(el => (Element: el, AvailableShrink: el.WidthMertrics.MinOuter - el.OuterBounds.Width))
                        .Where(item => item.AvailableShrink < 0)
                        .OrderByDescending(item => item.AvailableShrink).ToArray();
                    var totalShrink = shrinkElements.Sum(el => el.Element.FlexShrink);

                    foreach (var (element, availableShrink) in shrinkElements)
                    {
                        var share = remaining / totalShrink;
                        var alloc = Math.Max(availableShrink, share * element.FlexShrink);

                        element.SetOuterWidthClamped(element.OuterBounds.Width + alloc);

                        remaining -= alloc;
                        totalShrink -= element.FlexShrink;
                    }

                    break;
                }
            }

            line.UpdateMainSizeByRow(gap);
        }
    }



    #endregion
}
