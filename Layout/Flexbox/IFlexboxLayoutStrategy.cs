namespace SilkyUIFramework.Layout.Flexbox;

/// <summary>
/// Flexbox 布局策略接口
/// 封装不同方向（Row/Column）的布局算法
/// </summary>
public interface IFlexboxLayoutStrategy
{
    /// <summary>
    /// 计算换行
    /// </summary>
    /// <param name="context">布局上下文</param>
    void MeasureChildren(FlexboxContext context);

    /// <summary>
    /// 测量容器尺寸
    /// </summary>
    /// <param name="context">布局上下文</param>
    void Measure(FlexboxContext context);

    /// <summary>
    /// 调整子元素宽度
    /// </summary>
    /// <param name="context">布局上下文</param>
    void ResizeChildrenWidth(FlexboxContext context);

    /// <summary>
    /// 重新计算容器高度
    /// </summary>
    /// <param name="context">布局上下文</param>
    void RecalculateHeight(FlexboxContext context);

    /// <summary>
    /// 重新计算子元素高度
    /// </summary>
    /// <param name="context">布局上下文</param>
    void RecalculateChildrenHeight(FlexboxContext context);

    /// <summary>
    /// 调整子元素高度
    /// </summary>
    /// <param name="context">布局上下文</param>
    void ResizeChildrenHeight(FlexboxContext context);

    /// <summary>
    /// 更新子元素布局位置
    /// </summary>
    /// <param name="context">布局上下文</param>
    void UpdateChildrenLayoutPosition(FlexboxContext context);
}