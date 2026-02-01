namespace SilkyUIFramework.Layout;

public sealed partial class FlexboxModule
{
    private readonly List<FlexLine> _lines = [];

    private float MaxMainSize()
    {
        return _lines.Select(t => t.MainSize).Prepend(0f).Max();
    }

    private void SingleRow()
    {
        _lines.Clear();
        _lines.Add(FlexLine.CreateSingleRow(Parent.LayoutChildren, Parent.Gap.Width));
    }

    private void SingleColumn()
    {
        _lines.Clear();
        _lines.Add(FlexLine.CreateSingleColumn(Parent.LayoutChildren, Parent.Gap.Height));
    }

    private void WrapRow()
    {
        var width = Parent.InnerBounds.Width;
        var hGap = Parent.Gap.Width;
        var elements = Parent.LayoutChildren;
        _lines.Clear();

        var line = FlexLine.CreateRow(elements[0]);
        _lines.Add(line);

        for (var i = 1; i < elements.Count; i++)
        {
            var element = elements[i];

            if (line.MainSize + element.OuterBounds.Width + hGap <= width)
            {
                line.AddByRow(element, hGap);
                continue;
            }

            line = FlexLine.CreateRow(element);
            _lines.Add(line);
        }
    }

    private void WrapColumn()
    {
        var height = Parent.InnerBounds.Height;
        var vGap = Parent.Gap.Height;
        var elements = Parent.LayoutChildren;
        _lines.Clear();

        var line = FlexLine.CreateColumn(elements[0]);
        _lines.Add(line);

        for (var i = 1; i < elements.Count; i++)
        {
            var element = elements[i];

            if (line.MainSize + element.OuterBounds.Height + vGap <= height)
            {
                line.AddByColumn(element, vGap);
                continue;
            }

            line = FlexLine.CreateColumn(element);
            _lines.Add(line);
        }
    }

    /// <summary>
    /// 测量子元素大小
    /// </summary>
    private void MeasureSize(float gap, out float mainSize, out float crossSize)
    {
        mainSize = 0f;
        crossSize = (_lines.Count - 1) * gap;

        foreach (var line in _lines)
        {
            mainSize = Math.Max(mainSize, line.MainSize);
            crossSize += line.CrossSize;
        }
    }

    private void RowGrowOrShrink()
    {
        var width = Parent.InnerBounds.Width;
        var gap = Parent.Gap.Width;

        foreach (var line in _lines)
        {
            var remaining = width - line.MainSize;
            switch (remaining)
            {
                case > 0:
                {
                    var growElements = line.Elements
                        .Where(el => el.FlexGrow > 0)
                        .Select(el => (Element: el, AvailableGrowth: el.MaxOuterWidth - el.OuterBounds.Width))
                        .Where(item => item.AvailableGrowth > 0)
                        .OrderBy(item => item.AvailableGrowth).ToArray();
                    var totalGrow = growElements.Sum(el => el.Element.FlexGrow);

                    foreach (var (element, availableGrowth) in growElements)
                    {
                        var share = remaining / totalGrow;
                        var alloc = Math.Min(availableGrowth, share * element.FlexGrow);

                        SetOuterWidthClamped(element, element.OuterBounds.Width + alloc);

                        remaining -= alloc;
                        totalGrow -= element.FlexGrow;
                    }

                    break;
                }
                case < 0:
                {
                    var shrinkElements = line.Elements
                        .Where(el => el.FlexShrink > 0)
                        .Select(el => (Element: el, AvailableShrink: el.MinOuterWidth - el.OuterBounds.Width))
                        .Where(item => item.AvailableShrink < 0)
                        .OrderByDescending(item => item.AvailableShrink).ToArray();
                    var totalShrink = shrinkElements.Sum(el => el.Element.FlexShrink);

                    foreach (var (element, availableShrink) in shrinkElements)
                    {
                        var share = remaining / totalShrink;
                        var alloc = Math.Max(availableShrink, share * element.FlexShrink);

                        SetOuterWidthClamped(element, element.OuterBounds.Width + alloc);

                        remaining -= alloc;
                        totalShrink -= element.FlexShrink;
                    }

                    break;
                }
            }

            line.UpdateMainSizeByRow(gap);
        }
    }

    private void ColumnGrowOrShrink()
    {
        var height = Parent.InnerBounds.Height;
        foreach (var line in _lines)
        {
            var remaining = height - line.MainSize;

            switch (remaining)
            {
                case > 0:
                {
                    var sortedElements = line.Elements
                        .Where(el => el.FlexGrow > 0)
                        .Select(el => (Element: el, AvailableGrowth: el.MaxOuterHeight - el.OuterBounds.Height))
                        .Where(item => item.AvailableGrowth > 0)
                        .OrderBy(item => item.AvailableGrowth).ToArray();
                    var totalGrow = sortedElements.Sum(item => item.Element.FlexGrow);

                    foreach (var (element, availableGrowth) in sortedElements)
                    {
                        if (totalGrow <= 0) break;

                        var share = remaining / totalGrow;
                        var alloc = Math.Min(availableGrowth, share * element.FlexGrow);

                        SetOuterHeightClamped(element, element.OuterBounds.Height + alloc);

                        remaining -= alloc;
                        totalGrow -= element.FlexGrow;
                    }

                    line.UpdateMainSizeByColumn(Parent.Gap.Height);
                    break;
                }
                case < 0:
                {
                    var sortedElements = line.Elements
                        .Where(el => el.FlexShrink > 0)
                        .Select(el => (Element: el, AvailableShrink: el.MinOuterHeight - el.OuterBounds.Height))
                        .Where(item => item.AvailableShrink < 0)
                        .OrderByDescending(item => item.AvailableShrink).ToArray();
                    var totalShrink = sortedElements.Sum(el => el.Element.FlexShrink);

                    foreach (var (element, availableShrink) in sortedElements)
                    {
                        if (totalShrink <= 0 || remaining >= 0) break;

                        var share = remaining / totalShrink;
                        var alloc = Math.Max(availableShrink, share * element.FlexShrink);

                        SetOuterHeightClamped(element, element.OuterBounds.Height + alloc);

                        remaining -= alloc;
                        totalShrink -= element.FlexShrink;
                    }

                    line.UpdateMainSizeByColumn(Parent.Gap.Height);
                    break;
                }
            }
        }
    }

    private float _crossSize, _crossContent;

    private float UpdateCrossSize(float gap)
    {
        _crossContent = _lines.Sum(line => line.CrossSize);
        return _crossSize = _crossContent + (_lines.Count - 1) * gap;
    }

    private void UpdateCrossContentAlignment(float availableSize, float gap)
    {
        UpdateCrossSize(gap);
        switch (Parent.CrossContentAlignment)
        {
            default:
            case CrossContentAlignment.Start:
            case CrossContentAlignment.Stretch:
            {
                CrossOffsetCache = 0f;
                CrossGapCache = gap;
                return;
            }
            case CrossContentAlignment.Center:
            {
                CrossGapCache = gap;
                CrossOffsetCache = (availableSize - _crossSize) / 2f;
                return;
            }
            case CrossContentAlignment.End:
            {
                CrossGapCache = gap;
                CrossOffsetCache = availableSize - _crossSize;
                return;
            }
            case CrossContentAlignment.SpaceEvenly:
            {
                CrossGapCache = (availableSize - _crossContent) / (_lines.Count + 1);
                CrossOffsetCache = CrossGapCache;
                return;
            }
            case CrossContentAlignment.SpaceBetween:
            {
                CrossGapCache = _lines.Count > 1 ? (availableSize - _crossContent) / (_lines.Count - 1) : 0f;
                CrossOffsetCache = 0f;
                return;
            }
        }
    }

    private float CalculateCrossOffset(float availableSize, float itemCrossSize) => Parent.CrossAlignment switch
    {
        CrossAlignment.Center => (availableSize - itemCrossSize) / 2f,
        CrossAlignment.End => availableSize - itemCrossSize,
        CrossAlignment.Stretch or CrossAlignment.Start or { } => 0f,
    };
}