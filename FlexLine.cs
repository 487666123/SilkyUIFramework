using SilkyUIFramework.Layout;

namespace SilkyUIFramework;

public class FlexLine
{
    /// <summary>
    /// 数量永远不为 0，无需考虑为 0 的情况
    /// </summary>
    public IReadOnlyList<UIView> Elements => _elements;

    /// <summary>
    /// 数量永远不为 0，无需考虑为 0 的情况
    /// </summary>
    private readonly List<UIView> _elements;

    private FlexLine(UIView view) => _elements = [view];
    private FlexLine(IReadOnlyList<UIView> elements) => _elements = [.. elements];

    public float MainSize { get; set; }
    public float CrossSize { get; set; }

    public void AddByRow(UIView element, float gap)
    {
        _elements.Add(element);
        MainSize += element.OuterBounds.Width + gap;
        CrossSize = Math.Max(CrossSize, element.OuterBounds.Height);
    }

    public void AddByColumn(UIView element, float gap)
    {
        _elements.Add(element);
        MainSize += element.OuterBounds.Height + gap;
        CrossSize = Math.Max(CrossSize, element.OuterBounds.Width);
    }

    private float GetFenceGap(float gap) => (_elements.Count - 1) * gap;

    public float MaxOuterWidth()
    {
        return _elements.Select(t => t.OuterBounds.Width).Max();
    }

    public float MaxOuterHeight()
    {
        return _elements.Select(t => t.OuterBounds.Height).Max();
    }

    private float SumOuterWidth()
    {
        return _elements.Sum(t => t.OuterBounds.Width);
    }

    private float SumOuterHeight()
    {
        return _elements.Sum(t => t.OuterBounds.Height);
    }

    public void UpdateMainSizeByRow(float gap) => MainSize = SumOuterWidth() + GetFenceGap(gap);
    public void UpdateMainSizeByColumn(float gap) => MainSize = SumOuterHeight() + GetFenceGap(gap);

    public float MainOffset { get; private set; }

    /// <summary>
    /// 主轴间距
    /// </summary>
    public float MainGap { get; private set; }

    public void UpdateMainAlignment(MainAlignment mainAlignment, float availableSize, float baseGap)
    {
        if (_elements.Count == 0)
        {
            MainOffset = 0f;
            MainGap = baseGap;
            return;
        }

        switch (mainAlignment)
        {
            default:
            case MainAlignment.Start:
                MainOffset = 0f;
                MainGap = baseGap;
                break;
            case MainAlignment.Center:
                MainOffset = (availableSize - MainSize) / 2f;
                MainGap = baseGap;
                break;
            case MainAlignment.End:
                MainOffset = availableSize - MainSize;
                MainGap = baseGap;
                break;
            case MainAlignment.SpaceEvenly:
            {
                var contentSize = MainSize - baseGap * (_elements.Count - 1);
                MainGap = (availableSize - contentSize) / (_elements.Count + 1);
                MainOffset = MainGap;
                break;
            }
            case MainAlignment.SpaceBetween:
            {
                var contentSize = MainSize - baseGap * (_elements.Count - 1);
                if (_elements.Count > 1)
                {
                    MainGap = (availableSize - contentSize) / (_elements.Count - 1);
                    MainOffset = 0f;
                }
                else
                {
                    MainGap = 0f;
                    MainOffset = (availableSize - contentSize) / 2f;
                }

                break;
            }
        }
    }

    public static FlexLine CreateRow(UIView view)
    {
        var line = new FlexLine(view)
        {
            MainSize = view.OuterBounds.Width,
            CrossSize = view.OuterBounds.Height
        };

        return line;
    }

    public static FlexLine CreateColumn(UIView view)
    {
        var line = new FlexLine(view)
        {
            MainSize = view.OuterBounds.Height,
            CrossSize = view.OuterBounds.Width
        };

        return line;
    }

    public static FlexLine CreateSingleRow(IReadOnlyList<UIView> elements, float gap)
    {
        var line = new FlexLine(elements)
        {
            MainSize = elements.Sum(element => element.OuterBounds.Width) + (elements.Count - 1) * gap,
            CrossSize = elements.Max(element => element.OuterBounds.Height)
        };

        return line;
    }

    public static FlexLine CreateSingleColumn(IReadOnlyList<UIView> elements, float gap)
    {
        var line = new FlexLine(elements)
        {
            MainSize = elements.Sum(element => element.OuterBounds.Height) + (elements.Count - 1) * gap,
            CrossSize = elements.Max(element => element.OuterBounds.Width)
        };

        return line;
    }
}