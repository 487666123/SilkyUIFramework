using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

/// <summary> 滚动方向 </summary>
public enum Direction
{
    Horizontal,
    Vertical,
}

[XmlElementMapping("ScrollView")]
public class SUIScrollView : UIElementGroup
{
    public Direction Direction { get; }

    /// <summary>
    /// 滚动条
    /// </summary>
    public SUIScrollbar ScrollBar { get; }

    /// <summary>
    /// 遮罩层
    /// </summary>
    public SUIScrollMask Mask { get; }

    /// <summary>
    /// 容器
    /// </summary>
    public SUIScrollContainer Container { get; }

    public SUIScrollView(Direction direction = Direction.Vertical)
    {
        Direction = direction;
        Gap = new Size(8f);

        Mask = new SUIScrollMask(this)
        {
            HiddenBox = HiddenBox.Inner,
            FlexGrow = 1f,
            FlexShrink = 1f,
            Width = new Dimension(0f, 1f),
            Height = new Dimension(0f, 1f)
        };
        Mask.Join(this);

        Container = new SUIScrollContainer(this)
        {
            LayoutType = LayoutType.Flexbox,
            FlexDirection = FlexDirection.Row,
            MainAlignment = MainAlignment.SpaceBetween,
            FlexWrap = true,
            FitWidth = false,
            FitHeight = true,
            Width = new Dimension(0f, 1f),
            Height = new Dimension(0f, 1f),
            Gap = new Size(8f)
        }.Join(Mask);

        ScrollBar = new SUIScrollbar(direction, Container)
        {
            BorderRadius = new Vector4(2f),
            BackgroundColor = Color.Black * 0.25f,
            FitWidth = false,
            Width = new Dimension(8f),
            Height = new Dimension(0f, 1f)
        }.Join(this);
        ScrollBar.OnCurrentScrollPositionChanged += Container.UpdateScrollPosition;

        switch (Direction)
        {
            case Direction.Horizontal:
                FlexDirection = FlexDirection.Column;
                ScrollBar.SetSize(0f, 4f, 1f, 0f);
                break;
            default:
            case Direction.Vertical:
                FlexDirection = FlexDirection.Row;
                ScrollBar.SetSize(4f, 0f, 0f, 1f);
                break;
        }
    }

    public override void OnMouseWheel(UIScrollWheelEvent evt)
    {
        if (evt.ScrollingElement != null) return;

        switch (Direction)
        {
            case Direction.Horizontal:
            {
                if (evt.ScrollDelta > 0)
                {
                    if (ScrollBar.HScrolledToTop) break;
                }
                else if (ScrollBar.HScrolledToEnd) break;

                ScrollBar.HScrollBy(-evt.ScrollDelta);
                evt.ScrollingElement = this;
                break;
            }
            default:
            case Direction.Vertical:
            {
                if (evt.ScrollDelta > 0)
                {
                    if (ScrollBar.VScrolledToTop) break;
                }
                else if (ScrollBar.VScrolledToEnd) break;

                ScrollBar.VScrollBy(-evt.ScrollDelta);
                evt.ScrollingElement = this;
                break;
            }
        }

        base.OnMouseWheel(evt);
    }
}