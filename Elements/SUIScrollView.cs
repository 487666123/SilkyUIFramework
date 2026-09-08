using SilkyUIFramework.Common.Tweening;
using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

public class SUIScrollContainer(SUIScrollView scrollView) : UIElementGroup
{
    public SUIScrollView ScrollView { get; } = scrollView;

    public override void DrawChildren(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var innerBounds = ScrollView.Mask.InnerBounds;
        foreach (var child in ElementsInOrder.Where(el => el.OuterBounds.Intersects(innerBounds)))
        {
            child.HandleDraw(gameTime, spriteBatch);
        }
    }
}

/// <summary>
/// 滚动方向。
/// </summary>
public enum Orientation
{
    Horizontal,
    Vertical,
}

[XmlElementMapping("ScrollView")]
public class SUIScrollView : UIElementGroup
{
    #region Public Elements

    /// <summary>
    /// 当前滚动方向。
    /// </summary>
    public Orientation Orientation { get; }

    /// <summary>
    /// 滚动条。负责显示滚动进度，并在用户拖动滑块时发出滚动请求。
    /// </summary>
    public SUIScrollbar ScrollBar { get; }

    /// <summary>
    /// 视口遮罩。负责裁剪超出可视区域的内容，并在布局阶段更新滚动范围。
    /// </summary>
    public SUIScrollMask Mask { get; }

    /// <summary>
    /// 可滚动内容容器。滚动位置最终会转换为它的 <see cref="UIElementGroup.ScrollOffset"/>。
    /// </summary>
    public SUIScrollContainer Container { get; }

    #endregion

    #region Scroll Position

    private Tween _tween;

    /// <summary>
    /// 每次同步当前滚动位置后通知，包括重复赋值。
    /// </summary>
    public event Action<Vector2> ScrollPositionUpdated;

    /// <summary>
    /// 当前已经应用到内容容器的滚动位置。
    /// </summary>
    public virtual Vector2 CurrentScrollPosition
    {
        get;
        set
        {
            field = Vector2.Clamp(value, Vector2.Zero, MaxScrollPosition);

            // 内容位置和滚动条显示都由当前滚动位置驱动。
            Container.ScrollOffset = -field;
            SyncScrollBar();
            ScrollPositionUpdated?.Invoke(field);
        }
    }

    /// <summary>
    /// 将滚动目标设置到起点。
    /// </summary>
    public void ScrollToStart(bool animation = true) => ScrollTo(Vector2.Zero, animation);

    /// <summary>
    /// 将滚动目标设置到末尾。
    /// </summary>
    public void ScrollToEnd(bool animation = true) => ScrollTo(MaxScrollPosition, animation);

    /// <summary>
    /// 横向滚动指定距离。
    /// </summary>
    public void HScrollBy(float value) => ScrollBy(new Vector2(value, 0));

    /// <summary>
    /// 纵向滚动指定距离。
    /// </summary>
    public void VScrollBy(float value) => ScrollBy(new Vector2(0, value));

    /// <summary>
    /// 在当前滚动目标的基础上增加偏移量。
    /// </summary>
    public void ScrollBy(Vector2 offset, bool animation = true) => ScrollTo(CurrentScrollPosition + offset, animation);

    /// <summary>
    /// 设置平滑滚动目标位置。
    /// </summary>
    public void ScrollTo(Vector2 position, bool animation = true)
    {
        _tween?.Kill();
        if (animation)
        {
            var tween = _tween = CreateTween().Parallel().SetTrans(TransitionType.Quart).SetEase(EaseType.Out);
            tween.MemberTo(this, "CurrentScrollPosition", position, 0.2f);
            return;
        }

        CurrentScrollPosition = position;
    }

    #endregion

    #region Scroll Range

    /// <summary>
    /// 视口尺寸，由 <see cref="SUIScrollMask"/> 在布局阶段更新。
    /// </summary>
    public Vector2 ViewportSize { get; private set; } = Vector2.One;

    /// <summary>
    /// 内容尺寸，由 <see cref="SUIScrollMask"/> 在布局阶段更新。
    /// </summary>
    public Vector2 ContentSize { get; private set; } = Vector2.One;

    /// <summary>
    /// 当前内容相对于视口可移动的最大距离。
    /// </summary>
    public Vector2 MaxScrollPosition => Vector2.Max(Vector2.Zero, ContentSize - ViewportSize);

    /// <summary>
    /// 更新视口和内容尺寸，并重新钳制当前滚动位置。
    /// </summary>
    public void SetScrollSizes(Vector2 viewportSize, Vector2 contentSize)
    {
        ViewportSize = Vector2.Max(Vector2.One, viewportSize);
        ContentSize = Vector2.Max(Vector2.One, contentSize);
        CurrentScrollPosition = CurrentScrollPosition;
    }

    public void SetHorizontalScrollSizes(float viewportWidth, float contentWidth) =>
        SetScrollSizes(new Vector2(viewportWidth, 1f), new Vector2(contentWidth, 1f));

    public void SetVerticalScrollSizes(float viewportHeight, float contentHeight) =>
        SetScrollSizes(new Vector2(1f, viewportHeight), new Vector2(1f, contentHeight));

    #endregion

    #region Construction

    public SUIScrollView(Orientation orientation = Orientation.Vertical)
    {
        Orientation = orientation;
        Gap = new Size(8f);

        Mask = new SUIScrollMask(this)
        {
            HiddenBox = HiddenBox.Inner,
            FlexGrow = 1f,
            FlexShrink = 1f,
            Width = new Dimension(0f, 1f),
            Height = new Dimension(0f, 1f)
        }.Join(this);

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

        ScrollBar = new SUIScrollbar()
        {
            BorderRadius = new Vector4(2f),
            BackgroundColor = Color.Black * 0.25f,
            FitWidth = false,
            Width = new Dimension(8f),
            Height = new Dimension(0f, 1f)
        }.Join(this);

        // 滑块拖动只产生进度请求，滚动位置仍由 ScrollView 统一管理。
        ScrollBar.Drag += (_, vec2) =>
        {
            ScrollTo(MaxScrollPosition * vec2, false);
        };

        ConfigureOrientationLayout();
    }

    /// <summary>
    /// 根据滚动方向配置视图自身和滚动条的排列方式。
    /// </summary>
    private void ConfigureOrientationLayout()
    {
        switch (Orientation)
        {
            case Orientation.Horizontal:
            {
                FlexDirection = FlexDirection.Column;
                ScrollBar.SetSize(0f, 4f, 1f, 0f);
                break;
            }
            default:
            case Orientation.Vertical:
            {
                FlexDirection = FlexDirection.Row;
                ScrollBar.SetSize(4f, 0f, 0f, 1f);
                break;
            }
        }
    }

    #endregion

    #region Runtime Updates

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
    }

    /// <summary>
    /// 根据当前滚动位置同步滑块的尺寸比例和位置。
    /// </summary>
    private void SyncScrollBar()
    {
        ScrollBar.ViewportRatio = ViewportSize / ContentSize;
        ScrollBar.Value = Vector2.Clamp(CurrentScrollPosition / MaxScrollPosition, Vector2.Zero, Vector2.One);
    }

    #endregion

    #region Mouse Wheel

    public override void OnMouseWheel(UIScrollWheelEvent evt)
    {
        // 嵌套滚动视图已经消费的滚轮事件不能再次处理。
        if (evt.ScrollElement != null) return;

        // 当前视图到达边界时不锁定事件，让父级滚动视图继续处理。
        if (!CanScrollWithWheel(evt.ScrollDelta))
        {
            base.OnMouseWheel(evt);
            return;
        }

        if (Orientation == Orientation.Horizontal)
            HScrollBy(-evt.ScrollDelta);
        else
            VScrollBy(-evt.ScrollDelta);

        evt.LockScroll(this);
        base.OnMouseWheel(evt);
    }

    /// <summary>
    /// 判断当前视图是否能沿滚轮方向继续移动。
    /// </summary>
    private bool CanScrollWithWheel(int scrollDelta)
    {
        if (scrollDelta == 0) return false;

        var currentPosition = Orientation == Orientation.Horizontal
            ? CurrentScrollPosition.X
            : CurrentScrollPosition.Y;
        var maxPosition = Orientation == Orientation.Horizontal
            ? MaxScrollPosition.X
            : MaxScrollPosition.Y;

        return scrollDelta > 0 ? currentPosition > 0f : currentPosition < maxPosition;
    }

    #endregion
}
