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
            if (Positioning.IsFree)
            {
                UpdateLayoutFromFree();
            }
            else
            {
                UpdateLayoutFromFlow();
            }

            CleanupDirtyMark();
        }

        foreach (var child in ElementsCache)
        {
            child.UpdateLayout();
        }
    }

    protected void UpdateLayoutFromFree()
    {
        var container = GetParentInnerSpace();
        PreMeasure(container.Width, container.Height);
        ResizeChildrenWidth();
        RecalculateHeight();
        ResizeChildrenHeight();
        UpdateChildrenLayoutOffset();
    }

    protected void UpdateLayoutFromFlow()
    {
        PreMeasureChildren();
        ResizeChildrenWidth();
        RecalculateChildrenHeight();
        ResizeChildrenHeight();
        UpdateChildrenLayoutOffset();
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