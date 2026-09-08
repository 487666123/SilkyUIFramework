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
public enum Orientation { Horizontal, Vertical, }

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
    private Vector2 _targetScrollPosition;

    /// <summary>
    /// 当前滚动目标在有效范围内的投影，供普通滚动操作使用。
    /// </summary>
    public Vector2 TargetScrollPosition =>
        Vector2.Clamp(_targetScrollPosition, Vector2.Zero, MaxScrollPosition);

    /// <summary>
    /// 每次同步当前滚动位置后通知，包括重复赋值。
    /// </summary>
    public event Action<Vector2> ScrollPositionUpdated;

    /// <summary>
    /// 当前已经应用到内容容器的滚动位置。
    /// </summary>
    public virtual Vector2 ScrollPosition
    {
        get;
        set
        {
            field = value;
            Container.ScrollOffset = -field;
            ScrollPositionUpdated?.Invoke(field);
        }
    }

    #region ScrollTo

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
    /// 在目标滚动位置的基础上增加偏移量。
    /// </summary>
    public void ScrollBy(Vector2 offset, bool animation = true) =>
        ScrollTo(TargetScrollPosition + offset, animation);

    /// <summary>
    /// 设置平滑滚动目标位置。
    /// </summary>
    public void ScrollTo(Vector2 position, bool animation = true)
    {
        _tween?.Kill();
        if (animation)
        {
            var tween = _tween = CreateTween().Parallel().SetTrans(TransitionType.Quart).SetEase(EaseType.Out);
            tween.MemberTo(this, nameof(ScrollPosition), position, 0.15f);
            tween.OnCompleted += () =>
            {
                position = Vector2.Clamp(position, Vector2.Zero, MaxScrollPosition);
                if (ScrollPosition != position)
                {
                    tween = _tween = CreateTween().Parallel().SetTrans(TransitionType.Quart).SetEase(EaseType.Out);
                    tween.MemberTo(this, nameof(_targetScrollPosition), position, 0.15f);
                    tween.MemberTo(this, nameof(ScrollPosition), position, 0.15f);
                }
            };
            return;
        }

        position = Vector2.Clamp(position, Vector2.Zero, MaxScrollPosition);
        _targetScrollPosition = position;
        ScrollPosition = position;
    }

    #endregion

    /// <summary>
    /// 将未钳制的逻辑目标转换为实际显示位置。
    /// </summary>
    private Vector2 GetOverscrollPosition(Vector2 target)
    {
        var clampedTarget = Vector2.Clamp(target, Vector2.Zero, MaxScrollPosition);
        var overscroll = target - clampedTarget;
        return clampedTarget + new Vector2(
            GetOverscrollDistance(overscroll.X, ViewportSize.X),
            GetOverscrollDistance(overscroll.Y, ViewportSize.Y));
    }

    public float MaxOverscrollRatio { get; set; } = 1f;

    public float OverscrollResistance { get; set; } = 25f;

    /// <summary>
    /// 根据越界距离计算实际显示的橡皮筋位移。
    /// </summary>
    private float GetOverscrollDistance(float distance, float viewportSize)
    {
        if (distance == 0f) return 0f;

        var sign = MathF.Sign(distance);
        var normalizedDistance = MathF.Abs(distance) / viewportSize;

        var result = viewportSize * MaxOverscrollRatio *
                     normalizedDistance /
                     (OverscrollResistance + normalizedDistance);

        return sign * result;
    }

    #endregion

    #region Scroll Range

    /// <summary>
    /// 视口尺寸，由 <see cref="SUIScrollMask"/> 在布局阶段更新。
    /// </summary>
    public Vector2 ViewportSize
    {
        get; private set
        {
            field = Vector2.Max(Vector2.One, value);
        }
    } = Vector2.One;

    /// <summary>
    /// 内容尺寸，由 <see cref="SUIScrollMask"/> 在布局阶段更新。
    /// </summary>
    public Vector2 ContentSize
    {
        get; private set
        {
            field = Vector2.Max(Vector2.One, value);
        }
    } = Vector2.One;

    /// <summary>
    /// 当前内容相对于视口可移动的最大距离。
    /// </summary>
    public Vector2 MaxScrollPosition => Vector2.Max(Vector2.Zero, ContentSize - ViewportSize);

    /// <summary>
    /// 更新视口和内容尺寸，并将滚动目标钳制到新的有效范围。
    /// 不立即修改显示位置；若显示位置越界，后续更新会按回弹条件收回有效范围。
    /// </summary>
    public void SetScrollSizes(Vector2 viewportSize, Vector2 contentSize)
    {
        ViewportSize = viewportSize;
        ContentSize = contentSize;

        var clampedTarget = Vector2.Clamp(_targetScrollPosition, Vector2.Zero, MaxScrollPosition);
        ScrollTo(clampedTarget, false);
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

        ScrollPositionUpdated += SyncScrollBar;

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
    private void SyncScrollBar(Vector2 currentScrollPosition)
    {
        // 计算越界距离（两端都可能越界，取绝对值累加）
        var overscroll = Vector2.Max(Vector2.Zero, -currentScrollPosition) +
                         Vector2.Max(Vector2.Zero, currentScrollPosition - MaxScrollPosition);

        // 虚拟内容尺寸 = 原内容 + 越界距离，内容不足视口时保持满尺寸。
        var virtualContent = ContentSize + overscroll;

        ScrollBar.ViewportRatio = Vector2.Min(ViewportSize / virtualContent, Vector2.One);
        var max = MaxScrollPosition;
        var normalizedPosition = new Vector2(
            max.X > 0f ? currentScrollPosition.X / max.X : 0f,
            max.Y > 0f ? currentScrollPosition.Y / max.Y : 0f);
        ScrollBar.Value = Vector2.Clamp(normalizedPosition, Vector2.Zero, Vector2.One);
    }

    #endregion

    #region Mouse Wheel

    public override void OnMouseWheel(UIScrollWheelEvent evt)
    {
        if (evt.ScrollElement is null)
        {
            var offset = Orientation == Orientation.Horizontal
                ? new Vector2(-evt.ScrollDelta, 0)
                : new Vector2(0, -evt.ScrollDelta);

            _targetScrollPosition += offset;
            var displayPosition = GetOverscrollPosition(_targetScrollPosition);
            ScrollTo(displayPosition);

            if (CanScrollWithWheel(evt.ScrollDelta))
                evt.LockScroll(this);
        }

        base.OnMouseWheel(evt);
    }

    /// <summary>
    /// 仅判断逻辑目标能否在有效范围内沿滚轮方向移动，不考虑橡皮筋位移或动画进度。
    /// </summary>
    private bool CanScrollWithWheel(int scrollDelta)
    {
        if (scrollDelta == 0) return false;

        var max = Orientation == Orientation.Horizontal
            ? MaxScrollPosition.X
            : MaxScrollPosition.Y;
        if (max <= 0f) return false;

        var target = Orientation == Orientation.Horizontal
            ? TargetScrollPosition.X
            : TargetScrollPosition.Y;

        return scrollDelta > 0 ? target > 0f : target < max;
    }

    #endregion
}
