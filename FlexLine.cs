using SilkyUIFramework.Layout;

namespace SilkyUIFramework;

/// <summary>
/// 表示 Flex 布局中的一条布局线（Row 或 Column）及其尺寸信息。
/// </summary>
public class FlexLine
{
    /// <summary>
    /// 当前布局线中的元素集合。
    /// </summary>
    public IReadOnlyList<UIView> Elements => _elements;

    /// <summary>
    /// 布局线元素的可变存储。
    /// </summary>
    private readonly List<UIView> _elements;

    private FlexLine(UIView view) => _elements = [view];
    private FlexLine(IReadOnlyList<UIView> elements) => _elements = [.. elements];

    /// <summary>
    /// 主轴总尺寸（元素尺寸与间距之和）。
    /// </summary>
    public float MainSize { get; set; }

    /// <summary>
    /// 交叉轴最大尺寸。
    /// </summary>
    public float CrossSize { get; set; }

    /// <summary>
    /// 按 Row 方向追加元素，并增量更新主轴/交叉轴尺寸缓存。
    /// </summary>
    public void AddByRow(UIView element, float gap)
    {
        _elements.Add(element);
        MainSize += element.OuterBounds.Width + gap;
        CrossSize = Math.Max(CrossSize, element.OuterBounds.Height);
    }

    /// <summary>
    /// 按 Column 方向追加元素，并增量更新主轴/交叉轴尺寸缓存。
    /// </summary>
    public void AddByColumn(UIView element, float gap)
    {
        _elements.Add(element);
        MainSize += element.OuterBounds.Height + gap;
        CrossSize = Math.Max(CrossSize, element.OuterBounds.Width);
    }

    private float GetFenceGap(float gap) => (_elements.Count - 1) * gap;

    /// <summary>
    /// 返回布局线内元素外边界的最大宽度。
    /// </summary>
    public float MaxOuterWidth()
    {
        return _elements.Select(t => t.OuterBounds.Width).Max();
    }

    /// <summary>
    /// 返回布局线内元素外边界的最大高度。
    /// </summary>
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

    /// <summary>
    /// 按 Row 规则重算主轴总尺寸。
    /// </summary>
    public void UpdateMainSizeByRow(float gap) => MainSize = SumOuterWidth() + GetFenceGap(gap);

    /// <summary>
    /// 按 Column 规则重算主轴总尺寸。
    /// </summary>
    public void UpdateMainSizeByColumn(float gap) => MainSize = SumOuterHeight() + GetFenceGap(gap);

    /// <summary>
    /// 布局线在主轴上的起始偏移。
    /// </summary>
    public float MainOffset { get; private set; }

    /// <summary>
    /// 主轴方向的实际间距（应用对齐策略后）。
    /// </summary>
    public float MainGap { get; private set; }

    /// <summary>
    /// 根据主轴对齐策略计算 <see cref="MainOffset"/> 和 <see cref="MainGap"/>。
    /// </summary>
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

    /// <summary>
    /// 创建只包含一个元素的 Row 布局线。
    /// </summary>
    public static FlexLine CreateRow(UIView view)
    {
        var line = new FlexLine(view)
        {
            MainSize = view.OuterBounds.Width,
            CrossSize = view.OuterBounds.Height
        };

        return line;
    }

    /// <summary>
    /// 创建只包含一个元素的 Column 布局线。
    /// </summary>
    public static FlexLine CreateColumn(UIView view)
    {
        var line = new FlexLine(view)
        {
            MainSize = view.OuterBounds.Height,
            CrossSize = view.OuterBounds.Width
        };

        return line;
    }

    /// <summary>
    /// 由元素集合创建单条 Row 布局线，并一次性计算尺寸。
    /// </summary>
    public static FlexLine CreateSingleRow(IReadOnlyList<UIView> elements, float gap)
    {
        return new FlexLine(elements)
        {
            MainSize = elements.Sum(element => element.OuterBounds.Width) + (elements.Count - 1) * gap,
            CrossSize = elements.Max(element => element.OuterBounds.Height)
        };
    }

    /// <summary>
    /// 由元素集合创建单条 Column 布局线，并一次性计算尺寸。
    /// </summary>
    public static FlexLine CreateSingleColumn(IReadOnlyList<UIView> elements, float gap)
    {
        return new FlexLine(elements)
        {
            MainSize = elements.Sum(element => element.OuterBounds.Height) + (elements.Count - 1) * gap,
            CrossSize = elements.Max(element => element.OuterBounds.Width)
        };
    }
}