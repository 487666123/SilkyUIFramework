namespace SilkyUIFramework.Layout.Flexbox;

/// <summary>
/// 列方向（Column）的 Flexbox 布局策略
/// </summary>
public class ColumnLayoutStrategy : IFlexboxLayoutStrategy
{
    public static IFlexboxLayoutStrategy Instance { get; } = new ColumnLayoutStrategy();

    /// <inheritdoc />
    public void MeasureChildren(FlexboxContext context)
    {
        if (context.Container.FlexWrap && !context.Container.FitHeight)
            WrapColumn(context);
        else
            SingleColumn(context);
    }

    /// <inheritdoc />
    public void Measure(FlexboxContext context)
    {
        MeasureSize(context, context.Container.Gap.Height, out var mainSize, out var crossSize);
        if (context.Container.FitWidth) context.Container.SetInnerWidthClamped(crossSize);
        if (context.Container.FitHeight) context.Container.SetInnerHeightClamped(mainSize);
    }

    /// <inheritdoc />
    public void ResizeChildrenWidth(FlexboxContext context)
    {
        if (context.Container.CrossContentAlignment == CrossContentAlignment.Stretch)
        {
            var remaining = context.Container.InnerBounds.Width - FlexboxHelper.CalculateCrossSize(context.Lines, context.Container.Gap.Width);
            if (remaining > 0)
            {
                var share = remaining / context.Lines.Count;
                foreach (var line in context.Lines)
                    line.CrossSize += share;
            }
        }

        if (context.Container.CrossAlignment == CrossAlignment.Stretch)
        {
            foreach (var line in context.Lines)
            {
                foreach (var el in line.Elements.Where(el =>
                         el.Parent.FitWidth || !(el.OuterBounds.Width >= line.CrossSize)))
                {
                    el.SetOuterWidthClamped(line.CrossSize);
                }
            }
        }
    }

    /// <inheritdoc />
    public void RecalculateHeight(FlexboxContext context)
    {
        if (!context.Container.FitHeight) return;
        context.Container.SetInnerHeightClamped(context.GetMaxMainSize());
    }

    /// <inheritdoc />
    public void RecalculateChildrenHeight(FlexboxContext context)
    {
        foreach (var line in context.Lines)
            line.UpdateMainSizeByColumn(context.Container.Gap.Height);
    }

    /// <inheritdoc />
    public void ResizeChildrenHeight(FlexboxContext context)
    {
        if (context.Container.FlexWrap)
            WrapColumn(context);
        else
        {
            foreach (var line in context.Lines)
                line.UpdateMainSizeByColumn(context.Container.Gap.Height);
        }

        ColumnGrowOrShrink(context);

        var innerBounds = context.Container.InnerBounds;

        foreach (var line in context.Lines)
            line.UpdateMainAlignment(context.Container.MainAlignment, innerBounds.Height, context.Container.Gap.Height);

        FlexboxHelper.UpdateCrossContentAlignment(
            context.Lines,
            innerBounds.Width,
            context.Container.Gap.Width,
            context.Container.CrossContentAlignment,
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
            var top = line.MainOffset;

            foreach (var el in line.Elements)
            {
                var itemCrossOffset = FlexboxHelper.CalculateCrossOffset(line.CrossSize, el.OuterBounds.Width, context.Container.CrossAlignment);
                el.SetLayoutOffset(crossStart + itemCrossOffset, top);
                top += el.OuterBounds.Height + line.MainGap;
            }

            crossStart += line.CrossSize + context.CrossGap;
        }
    }

    #region 私有方法

    private static void SingleColumn(FlexboxContext context)
    {
        context.ClearLines();
        context.AddLine(FlexLine.CreateSingleColumn(context.Container.InFlowChildren, context.Container.Gap.Height));
    }

    private static void WrapColumn(FlexboxContext context)
    {
        var height = context.Container.InnerBounds.Height;
        var vGap = context.Container.Gap.Height;
        var elements = context.Container.InFlowChildren;
        context.ClearLines();

        if (elements.Count == 0) return;

        var line = FlexLine.CreateColumn(elements[0]);
        context.AddLine(line);

        for (var i = 1; i < elements.Count; i++)
        {
            var element = elements[i];

            if (line.MainSize + element.OuterBounds.Height + vGap <= height)
            {
                line.AddByColumn(element, vGap);
                continue;
            }

            line = FlexLine.CreateColumn(element);
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

    private static void ColumnGrowOrShrink(FlexboxContext context)
    {
        var mainSize = context.Container.InnerBounds.Height;

        foreach (var line in context.Lines)
        {
            var remaining = mainSize - line.MainSize;

            switch (remaining)
            {
                case > 0:
                {
                    var sortedElements = line.Elements
                        .Where(el => el.FlexGrow > 0)
                        .Select(el => (Element: el, AvailableGrowth: el.HeightMertrics.MaxOuter - el.OuterBounds.Height))
                        .Where(item => item.AvailableGrowth > 0)
                        .OrderBy(item => item.AvailableGrowth).ToArray();
                    var totalGrow = sortedElements.Sum(item => item.Element.FlexGrow);

                    foreach (var (element, availableGrowth) in sortedElements)
                    {
                        if (totalGrow <= 0) break;

                        var share = remaining / totalGrow;
                        var alloc = Math.Min(availableGrowth, share * element.FlexGrow);

                        element.SetOuterHeightClamped(element.OuterBounds.Height + alloc);

                        remaining -= alloc;
                        totalGrow -= element.FlexGrow;
                    }

                    line.UpdateMainSizeByColumn(context.Container.Gap.Height);
                    break;
                }
                case < 0:
                {
                    var sortedElements = line.Elements
                        .Where(el => el.FlexShrink > 0)
                        .Select(el => (Element: el, AvailableShrink: el.HeightMertrics.MinOuter - el.OuterBounds.Height))
                        .Where(item => item.AvailableShrink < 0)
                        .OrderByDescending(item => item.AvailableShrink).ToArray();
                    var totalShrink = sortedElements.Sum(el => el.Element.FlexShrink);

                    foreach (var (element, availableShrink) in sortedElements)
                    {
                        if (totalShrink <= 0 || remaining >= 0) break;

                        var share = remaining / totalShrink;
                        var alloc = Math.Max(availableShrink, share * element.FlexShrink);

                        element.SetOuterHeightClamped(element.OuterBounds.Height + alloc);

                        remaining -= alloc;
                        totalShrink -= element.FlexShrink;
                    }

                    line.UpdateMainSizeByColumn(context.Container.Gap.Height);
                    break;
                }
            }
        }
    }

    #endregion
}
