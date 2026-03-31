namespace SilkyUIFramework.Layout.Flexbox;

/// <summary>
/// Flexbox 布局辅助工具类，包含与方向无关的布局算法
/// </summary>
public static class FlexboxHelper
{
    /// <summary>
    /// 计算交叉轴总尺寸
    /// </summary>
    /// <param name="lines">布局线集合</param>
    /// <param name="gap">间距</param>
    /// <returns>交叉轴总尺寸</returns>
    public static float CalculateCrossSize(List<FlexLine> lines, float gap)
    {
        if (lines.Count == 0) return 0f;
        var crossContent = lines.Sum(line => line.CrossSize);
        return crossContent + (lines.Count - 1) * gap;
    }

    /// <summary>
    /// 更新交叉轴内容对齐方式
    /// </summary>
    /// <param name="lines">布局线集合</param>
    /// <param name="availableSize">可用尺寸</param>
    /// <param name="gap">间距</param>
    /// <param name="crossContentAlignment">交叉轴内容对齐方式</param>
    /// <param name="crossGapCache">输出的交叉轴间距缓存</param>
    /// <param name="crossOffsetCache">输出的交叉轴偏移缓存</param>
    public static void UpdateCrossContentAlignment(
        List<FlexLine> lines,
        float availableSize,
        float gap,
        CrossContentAlignment crossContentAlignment,
        out float crossGapCache,
        out float crossOffsetCache)
    {
        var crossContent = lines.Sum(line => line.CrossSize);
        var crossSize = crossContent + (lines.Count - 1) * gap;

        switch (crossContentAlignment)
        {
            default:
            case CrossContentAlignment.Start:
            case CrossContentAlignment.Stretch:
            {
                crossGapCache = gap;
                crossOffsetCache = 0f;
                return;
            }
            case CrossContentAlignment.Center:
            {
                crossGapCache = gap;
                crossOffsetCache = (availableSize - crossSize) / 2f;
                return;
            }
            case CrossContentAlignment.End:
            {
                crossGapCache = gap;
                crossOffsetCache = availableSize - crossSize;
                return;
            }
            case CrossContentAlignment.SpaceEvenly:
            {
                crossGapCache = (availableSize - crossContent) / (lines.Count + 1);
                crossOffsetCache = crossGapCache;
                return;
            }
            case CrossContentAlignment.SpaceBetween:
            {
                crossGapCache = lines.Count > 1 ? (availableSize - crossContent) / (lines.Count - 1) : 0f;
                crossOffsetCache = 0f;
                return;
            }
        }
    }

    /// <summary>
    /// 计算交叉轴偏移
    /// </summary>
    /// <param name="availableSize">可用尺寸</param>
    /// <param name="itemCrossSize">元素交叉轴尺寸</param>
    /// <param name="alignment">对齐方式</param>
    /// <returns>偏移量</returns>
    public static float CalculateCrossOffset(float availableSize, float itemCrossSize, CrossAlignment alignment) => alignment switch
    {
        CrossAlignment.Center => (availableSize - itemCrossSize) / 2f,
        CrossAlignment.End => availableSize - itemCrossSize,
        CrossAlignment.Stretch or CrossAlignment.Start or _ => 0f,
    };
}