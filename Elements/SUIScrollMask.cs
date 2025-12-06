namespace SilkyUIFramework.Elements;

public class SUIScrollMask : UIElementGroup
{
    public SUIScrollView ScrollView { get; }

    public SUIScrollMask(SUIScrollView scrollView)
    {
        ScrollView = scrollView;
        OverflowHidden = true;
    }

    protected override void RecalculateChildrenHeight()
    {
        base.RecalculateChildrenHeight();
    }

    protected override void ResizeChildrenHeight()
    {
        base.ResizeChildrenHeight();

        switch (ScrollView.Direction)
        {
            case Direction.Horizontal:
                ScrollView.ScrollBar?.SetHScrollRange(InnerBounds.Width, ScrollView.Container.OuterBounds.Width);
                break;
            default:
            case Direction.Vertical:
                ScrollView.ScrollBar?.SetVScrollRange(InnerBounds.Height, ScrollView.Container.OuterBounds.Height);
                break;
        }
    }

    public override UIView GetElementAt(Vector2 mousePosition)
    {
        if (DisableMouseInteraction) return null;
        if (!ContainsPoint(mousePosition)) return null;

        foreach (var child in ElementsInOrder.Reverse<UIView>())
        {
            var target = child.GetElementAt(mousePosition);
            if (target != null) return target;
        }

        // 所有子元素都不符合条件, 如果自身不忽略鼠标交互, 则返回自己
        return IgnoreMouseInteraction ? null : this;
    }
}
