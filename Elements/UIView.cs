namespace SilkyUIFramework.Elements;

[XmlElementMapping("View")]
public partial class UIView
{
    #region IgnoreMouseInteraction Invalid IsMouseHovering DirtyMark

    /// <summary> 忽略鼠标交互, 不影响其子元素交互 </summary>
    public bool IgnoreMouseInteraction { get; set; }

    /// <summary> 禁用鼠标交互, 影响其子元素交互 </summary>
    public bool DisableMouseInteraction { get; set; }

    public bool Invalid
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();

            Parent?.ElementsOrderIsDirty = true;
        }
    }

    public int ZIndex
    {
        get;
        set
        {
            if (field == value) return;
            field = value;

            Parent?.ElementsOrderIsDirty = true;
        }
    }

    public bool IsMouseHovering { get; set; }

    public bool LayoutIsDirty { get; protected set; } = true;

    public void MarkLayoutDirty()
    {
        LayoutIsDirty = true;
        MarkPositionDirty();

        // 如果元素是自由定位的，则不需要通知父元素
        if (Positioning.IsOutOfFlow) return;
        Parent?.NotifyParentChildDirty();
    }

    /// <summary>
    /// 清理 <see cref="LayoutIsDirty"/>
    /// </summary>
    public virtual void CleanupDirtyMark() => LayoutIsDirty = false;

    #endregion

    #region Parent Remove() ContainsPoint() GetElementAt()

    public UIElementGroup GetAncestor()
    {
        if (Parent == null) return null;

        var ancestor = Parent;

        while (ancestor.Parent != null)
        {
            ancestor = ancestor.Parent;
        }

        return ancestor;
    }

    /// <summary>
    /// 元素是否在 UI 树中
    /// </summary>
    public bool IsInsideTree => SilkyUI != null;

    public UIElementGroup Parent { get; protected internal set; }

    public virtual void RemoveFromParent() => Parent?.RemoveChild(this);

    public virtual bool ContainsPoint(Vector2 point) => Bounds.Contains(point);

    public virtual UIView GetElementAt(Vector2 mousePosition)
    {
        if (DisableMouseInteraction || IgnoreMouseInteraction) return null;

        return ContainsPoint(mousePosition) ? this : null;
    }

    #endregion

    private bool _initialized = false;

    /// <summary>
    /// 初始化元素
    /// </summary>
    internal virtual void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        OnInitialize();
    }

    protected virtual void OnInitialize() { }

    public SilkyUI SilkyUI { get; private set; }

    internal virtual void HandleEnterTree(SilkyUI silkyUI)
    {
        if (SilkyUI != null || silkyUI == null) return;
        SilkyUI = silkyUI;
        OnEnterTree();
    }

    internal virtual void HandleExitTree()
    {
        if (SilkyUI == null) return;
        SilkyUI = null;
        OnExitTree();
    }

    /// <summary> 当元素加入UI树中时调用 </summary>
    protected virtual void OnEnterTree() { }

    /// <summary> 当元素移出UI树中时调用 </summary>
    protected virtual void OnExitTree() { }

    #region PositionIsDirty

    protected bool PositionIsDirty { get; set; } = true;
    protected void MarkPositionDirty() => PositionIsDirty = true;
    protected void CleanupPositionDirtyMark() => PositionIsDirty = false;

    #endregion

    public Positioning Positioning
    {
        get;
        set
        {
            if (field == value) return;
            var isFree = field.IsOutOfFlow == value.IsOutOfFlow;

            field = value;
            MarkPositionDirty();

            if (isFree) return;
            LayoutIsDirty = true;
            Parent?.NotifyParentChildDirty();
        }
    }

    public StickyType StickyType
    {
        get;
        set
        {
            if (field == value) return;
            field = value;

            // Sticky 定位时，重新计算位置
            if (Positioning == Positioning.Sticky) MarkPositionDirty();
        }
    }

    public Vector4 Sticky
    {
        get;
        set
        {
            if (field == value) return;
            field = value;

            // Sticky 定位时，重新计算位置
            if (Positioning is Positioning.Sticky) MarkPositionDirty();
        }
    }

    /// <summary>
    /// 宽度、高度、位置如果有依赖父元素的则返回 true
    /// </summary>
    /// <returns></returns>
    public bool IsDependentParent()
    {
        return Width.Percent != 0 || Height.Percent != 0 ||
               Left.Percent != 0 || Top.Percent != 0 || Left.Alignment != 0 || Top.Alignment != 0;
    }

    #region Anchor

    private Anchor _left;
    private Anchor _top;

    public Anchor Left
    {
        get => _left;
        set => SetLeft(value.Pixels, value.Percent, value.Alignment);
    }

    public Anchor Top
    {
        get => _top;
        set => SetTop(value.Pixels, value.Percent, value.Alignment);
    }

    public void SetLeft(float? pixels = null, float? percent = null, float? alignment = null) =>
        SetAnchor(ref _left, pixels, percent, alignment);

    public void SetTop(float? pixels = null, float? percent = null, float? alignment = null) =>
        SetAnchor(ref _top, pixels, percent, alignment);

    private void SetAnchor(ref Anchor anchor, float? pixels = null, float? percent = null, float? alignment = null)
    {
        if (pixels == anchor.Pixels && percent == anchor.Percent && alignment == anchor.Alignment) return;
        anchor = anchor.With(pixels, percent, alignment);

        if (Positioning != Positioning.Static)
            MarkPositionDirty();
    }

    #endregion

    #region _layoutOffset _dragOffset

    private Vector2 _layoutOffset;

    public Vector2 LayoutOffset
    {
        get => _layoutOffset;
        set => SetLayoutOffset(value.X, value.Y);
    }

    protected internal void SetLayoutOffset(float? x = null, float? y = null)
    {
        if (x == _layoutOffset.X && y == _layoutOffset.Y) return;

        if (x.HasValue) _layoutOffset.X = x.Value;
        if (y.HasValue) _layoutOffset.Y = y.Value;

        MarkPositionDirty();
    }

    public Vector2 DragOffset
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;
            MarkPositionDirty();
        }
    }

    public void SetDragOffset(float? x = null, float? y = null) =>
        DragOffset = new Vector2(x ?? DragOffset.X, y ?? DragOffset.Y);

    #endregion

    public virtual void UpdatePosition()
    {
        if (PositionIsDirty)
        {
            RecalculatePosition();
        }
    }

    public virtual void RecalculatePosition()
    {
        switch (Positioning)
        {
            // 固定定位: 相对屏幕, 不受到布局影响
            case Positioning.Fixed:
            {
                var container = GraphicsDeviceHelper.GetBackBufferSizeByUIScale();
                OuterBounds.X = Left.CalculatePosition(container.Width, OuterBounds.Width) + DragOffset.X;
                OuterBounds.Y = Top.CalculatePosition(container.Height, OuterBounds.Height) + DragOffset.Y;
                break;
            }
            // 绝对定位: 相对父元素, 不受到布局影响
            case Positioning.Absolute:
            {
                var container = Parent?.InnerBounds ??
                                new Bounds(Vector2.Zero, GraphicsDeviceHelper.GetBackBufferSizeByUIScale());

                OuterBounds.X = container.X + Left.CalculatePosition(container.Width, OuterBounds.Width) +
                                DragOffset.X;
                OuterBounds.Y = container.Y + Top.CalculatePosition(container.Height, OuterBounds.Height) +
                                DragOffset.Y;
                break;
            }
            // 粘性定位 and 相对定位
            default:
            case Positioning.Sticky:
            case Positioning.Relative:
            {
                var container = Parent?.InnerBounds ??
                                new Bounds(Vector2.Zero, GraphicsDeviceHelper.GetBackBufferSizeByUIScale());

                var scrollOffset = GetScrollOffsetByParent();
                OuterBounds.X = container.X + scrollOffset.X +
                                Left.CalculatePosition(container.Width, OuterBounds.Width) +
                                LayoutOffset.X + DragOffset.X;
                OuterBounds.Y = container.Y + scrollOffset.Y +
                                Top.CalculatePosition(container.Height, OuterBounds.Height) +
                                LayoutOffset.Y + DragOffset.Y;
                HandleStickyPositioning(container);
                break;
            }
            // 完全由父元素控制
            case Positioning.Static:
            {
                var container = Parent?.InnerBounds ??
                                new Bounds(Vector2.Zero, GraphicsDeviceHelper.GetBackBufferSizeByUIScale());

                var scrollOffset = GetScrollOffsetByParent();
                OuterBounds.X = container.X + scrollOffset.X + LayoutOffset.X;
                OuterBounds.Y = container.Y + scrollOffset.Y + LayoutOffset.Y;
                break;
            }
        }

        Bounds.Position = OuterBounds.Position + Margin;
        InnerBounds.Position = Bounds.Position + new Vector2(Border) + Padding;

        CleanupPositionDirtyMark();
    }

    protected Vector2 GetScrollOffsetByParent()
    {
        return Parent?.ScrollOffset ?? Vector2.Zero;
    }

    protected virtual void HandleStickyPositioning(Bounds container)
    {
        if (Positioning != Positioning.Sticky) return;

        if (StickyType.HasFlag(StickyType.Left))
            OuterBounds.X = Math.Max(OuterBounds.X, container.X + Sticky.X);

        if (StickyType.HasFlag(StickyType.Top))
            OuterBounds.Y = Math.Max(OuterBounds.Y, container.Y + Sticky.Y);

        if (StickyType.HasFlag(StickyType.Right))
            OuterBounds.X = Math.Min(OuterBounds.X, container.X + container.Width - OuterBounds.Width - Sticky.Z);

        if (StickyType.HasFlag(StickyType.Bottom))
            OuterBounds.Y = Math.Min(OuterBounds.Y, container.Y + container.Height - OuterBounds.Height - Sticky.W);
    }

    /// <summary>
    /// 布局计算后的最终值
    /// </summary>
    public Bounds Bounds;

    /// <summary>
    /// 布局计算后的最终值
    /// </summary>
    public Bounds InnerBounds;

    /// <summary>
    /// 布局计算后的最终值
    /// </summary>
    public Bounds OuterBounds;

    public event Action<GameTime> OnUpdateStatus;

    public virtual void HandleUpdateStatus(GameTime gameTime)
    {
        OnUpdateStatus?.Invoke(gameTime);
        UpdateStatus(gameTime);
    }

    public event Action<GameTime> OnUpdate;
    protected virtual void Update(GameTime gameTime) { }

    public virtual void HandleUpdate(GameTime gameTime)
    {
        OnUpdate?.Invoke(gameTime);
        Update(gameTime);
    }
}