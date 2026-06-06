namespace SilkyUIFramework.Layout.Flexbox;

/// <summary>
/// Flexbox 布局上下文，包含共享数据和状态
/// </summary>
/// <remarks>
/// 创建新的布局上下文，包含空的布局线列表
/// </remarks>
/// <param name="parent">父容器</param>
public class FlexboxContext(UIElementGroup parent)
{
    /// <summary>
    /// 父容器元素
    /// </summary>
    public UIElementGroup Parent { get; } = parent;

    /// <summary>
    /// 布局线集合（可变内部存储）
    /// </summary>
    private readonly List<FlexLine> _lines = [];

    /// <summary>
    /// 布局线集合（只读视图）
    /// </summary>
    public IReadOnlyList<FlexLine> Lines => _lines;

    /// <summary>
    /// 交叉轴偏移缓存
    /// </summary>
    public float CrossOffset { get; set; } = 0f;

    /// <summary>
    /// 交叉轴间距缓存
    /// </summary>
    public float CrossGap { get; set; } = 0f;

    /// <summary>
    /// 清空布局线
    /// </summary>
    public void ClearLines()
    {
        _lines.Clear();
    }

    /// <summary>
    /// 添加布局线
    /// </summary>
    /// <param name="line">要添加的布局线</param>
    public void AddLine(FlexLine line)
    {
        _lines.Add(line);
    }

    /// <summary>
    /// 获取最大主轴尺寸
    /// </summary>
    /// <returns>最大主轴尺寸</returns>
    public float GetMaxMainSize()
    {
        if (Lines.Count == 0) return 0f;
        float max = 0f;
        foreach (var line in Lines)
        {
            if (line.MainSize > max) max = line.MainSize;
        }
        return max;
    }

}