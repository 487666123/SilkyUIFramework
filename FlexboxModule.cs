namespace SilkyUIFramework;

public static class FlexboxModule
{
    #region extensions List<UIView>

    extension(List<UIView> elements)
    {
        /// <summary>
        /// 对一组元素以行模式进行换行
        /// </summary>
        public void FlexWrapRow(List<FlexLine> flexLines, float maxMainAxisSize, float gap)
        {
            flexLines.Clear();
            if (elements.Count == 0) return;
            var element = elements[0];
            var flexLine = new FlexLine(element, element.OuterBounds.Width, element.OuterBounds.Height);

            for (var i = 1; i < elements.Count; i++)
            {
                element = elements[i];

                var mainAxisSize = element.OuterBounds.Width;
                var crossAxisSize = element.OuterBounds.Height;

                if (flexLine.MainSize + mainAxisSize + gap > maxMainAxisSize)
                {
                    flexLines.Add(flexLine);
                    flexLine = new FlexLine(element, mainAxisSize, crossAxisSize);
                    continue;
                }

                flexLine.Elements.Add(element);
                flexLine.MainSize += mainAxisSize + gap;
                flexLine.CrossSize = MathF.Max(flexLine.CrossSize, crossAxisSize);
            }

            flexLines.Add(flexLine);
        }

        /// <summary>
        /// 对一组元素以列模式进行换行
        /// </summary>
        public void FlexWrapColumn(List<FlexLine> flexLines, float maxMainAxisSize, float gap)
        {
            flexLines.Clear();
            if (elements.Count == 0) return;
            var element = elements[0];
            var flexLine = new FlexLine(element, element.OuterBounds.Height, element.OuterBounds.Width);

            for (var i = 1; i < elements.Count; i++)
            {
                element = elements[i];

                var mainAxisSize = element.OuterBounds.Height;
                var crossAxisSize = element.OuterBounds.Width;

                if (flexLine.MainSize + mainAxisSize + gap > maxMainAxisSize)
                {
                    flexLines.Add(flexLine);
                    flexLine = new FlexLine(element, mainAxisSize, crossAxisSize);
                    continue;
                }

                flexLine.Elements.Add(element);
                flexLine.MainSize += mainAxisSize + gap;
                flexLine.CrossSize = MathF.Max(flexLine.CrossSize, crossAxisSize);
            }

            flexLines.Add(flexLine);
        }
    }

    #endregion

    public static void MeasureSize(this List<FlexLine> flexLines, float gap, out float mainAxisSize, out float crossAxisSize)
    {
        mainAxisSize = 0f;
        crossAxisSize = 0f;

        foreach (var flexLine in flexLines)
        {
            mainAxisSize = Math.Max(mainAxisSize, flexLine.MainSize);
            crossAxisSize += flexLine.CrossSize;
        }

        crossAxisSize += (flexLines.Count - 1) * gap;
    }

    public static void GrowOrShrinkByRowMode(this List<FlexLine> flexLines, Size innerSize, float gap)
    {
        foreach (var flexLine in flexLines)
        {
            var remaining = innerSize.Width - flexLine.MainSize;
            switch (remaining)
            {
                case > 0:
                {
                    var growElements = flexLine.Elements
                        .Where(el => el.FlexGrow > 0)
                        .Select(el => new { Element = el, AvailableGrowth = el.MaxOuterWidth - el.OuterBounds.Width })
                        .Where(el => el.AvailableGrowth > 0)
                        .OrderBy(item => item.AvailableGrowth)
                        .ToList();
                    var totalGrow = growElements.Sum(el => el.Element.FlexGrow);

                    foreach (var item in growElements)
                    {
                        var each = remaining / totalGrow;
                        var alloc = Math.Min(item.AvailableGrowth, each * item.Element.FlexGrow);

                        item.Element.SetExactWidth(item.Element.OuterBounds.Width + alloc);

                        remaining -= alloc;
                        totalGrow -= item.Element.FlexGrow;
                    }

                    break;
                }
                case < 0:
                {
                    var shrinkElements = flexLine.Elements
                        .Where(el => el.FlexShrink > 0)
                        .Select(el => new { Element = el, AvailableShrink = el.MinOuterWidth - el.OuterBounds.Width })
                        .Where(el => el.AvailableShrink < 0)
                        .OrderByDescending(item => item.AvailableShrink)
                        .ToList();
                    var totalShrink = shrinkElements.Sum(el => el.Element.FlexShrink);

                    foreach (var item in shrinkElements)
                    {
                        var each = remaining / totalShrink;
                        var alloc = Math.Max(item.AvailableShrink, each * item.Element.FlexShrink);

                        item.Element.SetExactWidth(item.Element.OuterBounds.Width + alloc);

                        remaining -= alloc;
                        totalShrink -= item.Element.FlexShrink;
                    }
                    break;
                }
                default: break;
            }

            flexLine.MainSize = flexLine.Elements.Sum(el => el.OuterBounds.Width) + flexLine.GetFenceGap(gap);
        }
    }

    public static void GrowOrShrinkByColumnMode(this List<FlexLine> flexLines, Size innerSize, float gap)
    {
        // 主轴 grow 或 shrink
        foreach (var flexLine in flexLines)
        {
            var remaining = innerSize.Height - flexLine.MainSize;

            switch (remaining)
            {
                case > 0:
                {
                    var sortedElements = flexLine.Elements
                        .Where(el => el.FlexGrow > 0)
                        .Select(el => new { Element = el, AvailableGrowth = el.MaxOuterHeight - el.OuterBounds.Height })
                        .Where(item => item.AvailableGrowth > 0)
                        .OrderBy(item => item.AvailableGrowth)
                        .ToList();
                    var totalGrow = sortedElements.Sum(item => item.Element.FlexGrow);

                    foreach (var item in sortedElements)
                    {
                        if (totalGrow <= 0) break;

                        var each = remaining / totalGrow;
                        var alloc = Math.Min(item.AvailableGrowth, each * item.Element.FlexGrow);

                        item.Element.SetExactHeight(item.Element.OuterBounds.Height + alloc);

                        remaining -= alloc;
                        totalGrow -= item.Element.FlexGrow;
                    }

                    flexLine.MainSize = flexLine.Elements.Sum(el => el.OuterBounds.Height) + flexLine.GetFenceGap(gap);

                    break;
                }
                case < 0:
                {
                    var sortedElements = flexLine.Elements
                        .Where(el => el.FlexShrink > 0)
                        .Select(el => new { Element = el, AvailableShrink = el.MinOuterHeight - el.OuterBounds.Height })
                        .Where(el => el.AvailableShrink < 0)
                        .OrderByDescending(item => item.AvailableShrink)
                        .ToList();
                    var totalShrink = sortedElements.Sum(el => el.Element.FlexShrink);

                    foreach (var item in sortedElements)
                    {
                        if (totalShrink <= 0 || remaining >= 0) break;

                        var each = remaining / totalShrink;
                        var alloc = Math.Max(item.AvailableShrink, each * item.Element.FlexShrink);

                        item.Element.SetExactHeight(item.Element.OuterBounds.Height + alloc);

                        //Main.NewText($"Height: {item.Element.OuterBounds.Height} alloc: {alloc} each: {each} totalShrink:{totalShrink} remaining:{remaining}");

                        remaining -= alloc;
                        totalShrink -= item.Element.FlexShrink;
                    }

                    flexLine.MainSize = flexLine.Elements.Sum(el => el.OuterBounds.Height) + flexLine.GetFenceGap(gap);
                    break;
                }
                default: break;
            }
        }
    }

    public static void ResizeCrossAxis(this List<FlexLine> flexLines,
        CrossContentAlignment crossContentAlignment, float crossAxisSpace, float crossGap)
    {
        foreach (var flexLine in flexLines)
        {
            flexLine.CrossSpace = flexLine.CrossSize;
        }

        if (crossContentAlignment != CrossContentAlignment.Stretch) return;

        var remaining = crossAxisSpace - flexLines.CalculateCrossSize(crossGap);
        if (remaining <= 0) return;

        var each = remaining / flexLines.Count;
        foreach (var flexLine in flexLines)
        {
            flexLine.CrossSpace += each;
        }
    }

    public static float CalculateCrossAxisContent(this List<FlexLine> flexLines) => flexLines.Sum(flexLine => flexLine.CrossSize);

    public static float FenceGap(this List<FlexLine> flexLines, float gap) => (flexLines.Count - 1) * gap;

    public static float CalculateCrossSize(this List<FlexLine> flexLines, float gap) =>
        flexLines.CalculateCrossAxisContent() + flexLines.FenceGap(gap);

    public static void CalculateCrossContentAlignment(this List<FlexLine> flexLines,
        CrossContentAlignment crossContentAlignment, float containerSize, float gapSize, out float startOffset, out float crossGap)
    {
        switch (crossContentAlignment)
        {
            default:
            case CrossContentAlignment.Start:
            case CrossContentAlignment.Stretch:
            {
                startOffset = 0f;
                crossGap = gapSize;
                return;
            }
            case CrossContentAlignment.Center:
            {
                crossGap = gapSize;
                var crossContentSize = flexLines.CalculateCrossAxisContent() + flexLines.FenceGap(gapSize);
                startOffset = (containerSize - crossContentSize) / 2f;
                return;
            }
            case CrossContentAlignment.End:
            {
                crossGap = gapSize;
                var crossContentSize = flexLines.CalculateCrossAxisContent() + flexLines.FenceGap(gapSize);
                startOffset = containerSize - crossContentSize;
                return;
            }
            case CrossContentAlignment.SpaceEvenly:
            {
                var crossContentSize = flexLines.CalculateCrossAxisContent();
                crossGap = (containerSize - crossContentSize) / (flexLines.Count + 1);
                startOffset = crossGap;
                return;
            }
            case CrossContentAlignment.SpaceBetween:
            {
                var crossContentSize = flexLines.CalculateCrossAxisContent();
                crossGap = flexLines.Count > 1 ? (containerSize - crossContentSize) / (flexLines.Count - 1) : 0f;
                startOffset = 0f;
                return;
            }
        }
    }

    public static void CalculateMainAlignment(this FlexLine flexLine,
        float availableMainSize, float baseGap, MainAlignment mainAlignment, out float mainStartOffset,
        out float adjustedGap)
    {
        int elementCount = flexLine.Elements.Count;
        if (elementCount == 0)
        {
            mainStartOffset = 0f;
            adjustedGap = baseGap;
            return;
        }

        float totalBaseGap = baseGap * (elementCount - 1);

        switch (mainAlignment)
        {
            default:
            case MainAlignment.Start:
                mainStartOffset = 0f;
                adjustedGap = baseGap;
                break;
            case MainAlignment.Center:
                mainStartOffset = (availableMainSize - flexLine.MainSize) / 2f;
                adjustedGap = baseGap;
                break;
            case MainAlignment.End:
                mainStartOffset = availableMainSize - flexLine.MainSize;
                adjustedGap = baseGap;
                break;
            case MainAlignment.SpaceEvenly:
            {
                var totalContentWithoutGaps = flexLine.MainSize - totalBaseGap;
                adjustedGap = (availableMainSize - totalContentWithoutGaps) / (elementCount + 1);
                mainStartOffset = adjustedGap;
                break;
            }
            case MainAlignment.SpaceBetween:
            {
                var totalContentWithoutGaps = flexLine.MainSize - totalBaseGap;
                if (elementCount > 1)
                {
                    adjustedGap = (availableMainSize - totalContentWithoutGaps) / (elementCount - 1);
                    mainStartOffset = 0f;
                }
                else
                {
                    adjustedGap = 0f;
                    mainStartOffset = (availableMainSize - totalContentWithoutGaps) / 2f;
                }

                break;
            }
        }
    }

    public static float CalculateCrossOffset(this CrossAlignment crossAlignment, float crossAvailableSize, float itemCrossSize) => crossAlignment switch
    {
        CrossAlignment.Center => (crossAvailableSize - itemCrossSize) / 2f,
        CrossAlignment.End => crossAvailableSize - itemCrossSize,
        CrossAlignment.Stretch or CrossAlignment.Start or _ => 0f,
    };
}