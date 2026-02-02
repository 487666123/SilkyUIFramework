namespace SilkyUIFramework.Elements;

public partial class UIElementGroup
{
    protected internal virtual void NotifyParentChildDirty()
    {
        LayoutIsDirty = true;
        PositionIsDirty = true;

        if (FitWidth || FitHeight)
        {
            Parent?.NotifyParentChildDirty();
        }
    }

    #region Elements Z Index Order

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

    #endregion

    public override void UpdateLayout()
    {
        if (LayoutIsDirty)
        {
            if (Positioning.IsFree) UpdateBoxLayout();
            else UpdateFlowLayout();

            CleanupDirtyMark();
        }

        foreach (var child in ElementsCache)
        {
            child.UpdateLayout();
        }
    }

    /// <summary>
    /// 正常的布局计算
    /// </summary>
    protected void UpdateBoxLayout()
    {
        var container = GetParentInnerSpace();
        Measure(container.Width, container.Height);
        ResizeChildrenWidth();
        RecalculateHeight();
        ResizeChildrenHeight();
        UpdateChildrenLayoutPosition();
    }

    /// <summary>
    /// 设计的是一个在流中的更新，但是我想遗弃了，遗弃了设计起来也会更简单，留着其实也没什么大用的，最初设计是为了节省性能
    /// </summary>
    protected void UpdateFlowLayout()
    {
        MeasureChildren();
        ResizeChildrenWidth();
        RecalculateChildrenHeight();
        ResizeChildrenHeight();
        UpdateChildrenLayoutPosition();
    }

    protected void MarkFreeElementsDirty()
    {
        foreach (var item in FreeElements.Where(e => e.IsDependentParent() && !e.LayoutIsDirty))
        {
            item.MarkLayoutDirty();
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

    public override void RecalculatePosition()
    {
        base.RecalculatePosition();

        foreach (var child in ElementsCache.Where(el => el.Positioning != Positioning.Fixed))
        {
            child.RecalculatePosition();
        }
    }
}