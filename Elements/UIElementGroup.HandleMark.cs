namespace SilkyUIFramework.Elements;

public partial class UIElementGroup
{
    public bool ElementsOrderIsDirty { get; set; } = true;

    protected List<UIView> ElementsInOrder { get; } = [];

    public void UpdateElementsOrder()
    {
        if (ElementsOrderIsDirty)
        {
            ElementsInOrder.Clear();
            ElementsInOrder.AddRange(ElementsCache.OrderBy(el => el.ZIndex));
            ElementsOrderIsDirty = false;
        }

        foreach (var item in ElementsInOrder.OfType<UIElementGroup>())
        {
            item.UpdateElementsOrder();
        }
    }

    #region 同级别

    public override void UpdateLayout()
    {
        if (LayoutIsDirty)
        {
            HandleDirtyLayoutUpdate();
            CleanupDirtyMark();
        }

        foreach (var child in ElementsCache)
        {
            child.UpdateLayout();
        }
    }

    public override void UpdatePosition()
    {
        base.UpdatePosition();

        foreach (var child in ElementsCache)
        {
            child.UpdatePosition();
        }
    }

    #endregion

    /// <summary>
    /// 处理当前元素的布局脏标记。
    /// 默认仅脱离文档流元素需要独立执行完整布局管线；
    /// 在流内元素的布局由父容器布局阶段统一驱动。
    /// </summary>
    protected virtual void HandleDirtyLayoutUpdate()
    {
        if (!Positioning.IsOutOfFlow) return;

        RunIndependentLayoutPass();
    }

    /// <summary>
    /// 以当前容器为根执行一轮完整布局管线。
    /// </summary>
    protected void RunIndependentLayoutPass()
    {
        var container = GetAvailableSize();
        Measure(container.Width, container.Height);
        ResizeChildrenWidth();
        RecalculateHeight();
        ResizeChildrenHeight();
        UpdateChildrenLayoutPosition();
    }

    // <summary>
    // [已废弃]<br/>
    // 设计的是一个在流中的更新，但是我想遗弃了，遗弃了设计起来也会更简单，留着其实也没什么大用的，最初设计是为了节省性能
    // </summary>
    //protected void UpdateFlowLayout()
    //{
    //    MeasureChildren();
    //    ResizeChildrenWidth();
    //    RecalculateChildrenHeight();
    //    ResizeChildrenHeight();
    //    UpdateChildrenLayoutPosition();
    //}

    protected void MarkFreeElementsDirty()
    {
        foreach (var child in OutOfFlowElements.Where(e => e.IsDependentParent() && !e.LayoutIsDirty))
        {
            child.MarkLayoutDirty();
        }
    }

    public override void RecalculatePosition()
    {
        base.RecalculatePosition();

        foreach (var child in ElementsCache.Where(el => el.Positioning != Positioning.Fixed))
        {
            child.RecalculatePosition();
        }
    }
}
