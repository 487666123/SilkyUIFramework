namespace SilkyUIFramework.Layout;

/// <summary>
/// 布局模型<br/>
/// 实现此接口的类型不应暴漏任何其他方法
/// </summary>
public abstract class LayoutModule(UIElementGroup parent)
{
    public readonly UIElementGroup Parent = parent;
    protected Size Gap;
    protected bool FitWidth, FitHeight;

    /// <summary>
    /// 更新缓存状态, 将父元素中布局计算相关值复制到本类 (能避免一次寻址 (管他有没有用，就这么搞了))
    /// </summary>
    public virtual void UpdateCacheStatus()
    {
        Gap = Parent.Gap;
        FitWidth = Parent.FitWidth;
        FitHeight = Parent.FitHeight;
    }

    /// <summary>
    /// 通常在 FitWidth 或 FitHeight 有为 true 时，直接设定元素大小
    /// </summary>
    public virtual void PreMeasure() { }

    /// <summary>
    /// 修改子元素可用空间, 初始分配时调用
    /// </summary>
    public virtual void ModifyAvailableSize(UIView view, int index,
        ref float? availableWidth, ref float? availableHeight)
    { }

    /// <summary>
    /// 通常用于统计一些子元素的信息<br/>
    /// 在 <see cref="PreMeasure"/> 之前调用
    /// </summary>
    public virtual void PreMeasureChildren() { }
    public virtual void ResizeChildrenWidth() { }
    public virtual void RecalculateHeight() { }
    public virtual void RecalculateChildrenHeight() { }
    public virtual void ResizeChildrenHeight() { }
    public virtual void ModifyLayoutOffset() { }

    #region SetBounds Methods

    /// <summary>
    /// 用于 PreMeasure 阶段直接设置 OuterBounds.Width
    /// </summary>
    protected static void SetInnerWidthClamped(UIView target, float width)
    {
        target.SetInnerBoundsWidthRaw(MathHelper.Clamp(width, target.MinInnerWidth, target.MaxInnerWidth));
    }

    /// <summary>
    /// 用于 PreMeasure 阶段直接设置 OuterBounds.Height
    /// </summary>
    protected static void SetInnerHeightClamped(UIView target, float height)
    {
        target.SetInnerBoundsHeightRaw(MathHelper.Clamp(height, target.MinInnerHeight, target.MaxInnerHeight));
    }

    /// <summary>
    /// 通常用于 OnResizeChildrenWidth 阶段直接设置 OuterBounds.Width
    /// </summary>
    protected static void SetOuterWidthClamped(UIView target, float width)
    {
        target.SetOuterBoundsWidthRaw(MathHelper.Clamp(width, target.MinOuterWidth, target.MaxOuterWidth));
    }

    /// <summary>
    /// 通常用于 OnResizeChildrenHeight 阶段直接设置 OuterBounds.Height
    /// </summary>
    protected static void SetOuterHeightClamped(UIView target, float height)
    {
        target.SetOuterBoundsHeightRaw(MathHelper.Clamp(height, target.MinOuterHeight, target.MaxOuterHeight));
    }

    #endregion
}