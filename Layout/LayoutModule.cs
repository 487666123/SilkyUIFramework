namespace SilkyUIFramework.Layout;

/// <summary>
/// 布局模块
/// </summary>
public abstract class LayoutModule(UIElementGroup parent)
{
    public readonly UIElementGroup Parent = parent;

    /// <summary>
    /// 准备数据, 计算开始前
    /// </summary>
    public virtual void PrepareData() { }

    /// <summary>
    /// 测量完子元素后调用
    /// </summary>
    public virtual void MeasureChildren() { }

    /// <summary>
    /// 测量完自身后调用
    /// </summary>
    public virtual void Measure() { }

    public virtual void ResizeChildrenWidth()
    {
        // 固宽, 更子宽
        if (Parent.FitWidth) return;

        var width = Parent.InnerBounds.Width;
        foreach (var element in Parent.LayoutChildren)
        {
            element.UpdateWidth(width);
        }
    }
    public virtual void RecalculateHeight() { }
    public virtual void RecalculateChildrenHeight() { }
    public virtual void ResizeChildrenHeight()
    {
        // 固高, 更子高
        if (Parent.FitHeight) return;

        var height = Parent.InnerBounds.Height;
        foreach (var element in Parent.LayoutChildren)
        {
            element.UpdateHeight(height);
        }
    }
    public virtual void UpdateChildrenLayoutPosition() { }

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