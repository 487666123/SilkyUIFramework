namespace SilkyUIFramework.Layout.Flexbox;

/// <summary>
/// Flexbox 布局上下文，包含共享数据和状态
/// </summary>
/// <remarks>
/// 创建新的布局上下文，使用现有的布局线列表
/// </remarks>
/// <param name="parent">父容器</param>
/// <param name="lines">现有的布局线列表</param>
public class FlexboxContext(UIElementGroup parent)
{
    /// <summary>
    /// 父容器元素
    /// </summary>
    public UIElementGroup Parent { get; } = parent;

    /// <summary>
    /// 布局线集合
    /// </summary>
    public List<FlexLine> Lines { get; } = [];

    /// <summary>
    /// 交叉轴偏移缓存
    /// </summary>
    public float CrossOffsetCache { get; set; } = 0f;

    /// <summary>
    /// 交叉轴间距缓存
    /// </summary>
    public float CrossGapCache { get; set; } = 0f;

    /// <summary>
    /// 清空布局线
    /// </summary>
    public void ClearLines()
    {
        Lines.Clear();
    }

    /// <summary>
    /// 添加布局线
    /// </summary>
    /// <param name="line">要添加的布局线</param>
    public void AddLine(FlexLine line)
    {
        Lines.Add(line);
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

    /// <summary>
    /// 计算交叉轴总尺寸
    /// </summary>
    /// <param name="gap">间距</param>
    /// <returns>交叉轴总尺寸</returns>
    public float CalculateCrossSize(float gap)
    {
        if (Lines.Count == 0) return 0f;
        float crossContent = 0f;
        foreach (var line in Lines)
        {
            crossContent += line.CrossSize;
        }
        return crossContent + (Lines.Count - 1) * gap;
    }
}