namespace SilkyUIFramework.Elements;

public partial class UIView
{
    #region BoxSizing Margin Padding Border

    public BoxSizing BoxSizing
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public Margin Margin
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public void SetMargin(float margin) => Margin = new Margin(margin);

    public void SetMargin(float leftAndRight, float topAndBottom) =>
        Margin = new Margin(leftAndRight, topAndBottom, leftAndRight, topAndBottom);

    public Margin Padding
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public void SetPadding(float padding) => Padding = padding;

    public void SetPadding(float leftAndRight, float topAndBottom) =>
        Padding = new Margin(leftAndRight, topAndBottom, leftAndRight, topAndBottom);

    public float Border
    {
        get => RectangleRender.Border;
        set
        {
            if (RectangleRender.Border == value) return;
            RectangleRender.Border = value;
            MarkLayoutDirty();
        }
    }

    #endregion

    #region Fit Width Height

    public bool FitWidth
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public bool FitHeight
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    private Dimension _minWidth;
    private Dimension _maxWidth = new(ushort.MaxValue * 100);
    private Dimension _width;

    private Dimension _minHeight;
    private Dimension _maxHeight = new(ushort.MaxValue * 100);
    private Dimension _height;

    #endregion

    #region SetDimension

    private void SetDimension(ref Dimension dimension, float? pixels = null, float? percent = null)
    {
        if ((pixels.HasValue && dimension.Pixels != pixels.Value) ||
            (percent.HasValue && dimension.Percent != percent.Value))
        {
            dimension = dimension.With(pixels, percent);
            MarkLayoutDirty();
        }
    }

    public void SetWidth(float? pixels = null, float? percent = null) =>
        SetDimension(ref _width, pixels, percent);

    public void SetHeight(float? pixels = null, float? percent = null) =>
        SetDimension(ref _height, pixels, percent);

    public void SetMinWidth(float? pixels = null, float? percent = null) =>
        SetDimension(ref _minWidth, pixels, percent);

    public void SetMaxWidth(float? pixels = null, float? percent = null) =>
        SetDimension(ref _maxWidth, pixels, percent);

    public void SetMinHeight(float? pixels = null, float? percent = null) =>
        SetDimension(ref _minHeight, pixels, percent);

    public void SetMaxHeight(float? pixels = null, float? percent = null) =>
        SetDimension(ref _maxHeight, pixels, percent);

    public void SetSize(float? widthPixels = null, float? heightPixels = null, float? widthPercent = null,
        float? heightPercent = null)
    {
        SetWidth(widthPixels, widthPercent);
        SetHeight(heightPixels, heightPercent);
    }

    public Dimension Width
    {
        get => _width;
        set { SetWidth(value.Pixels, value.Percent); }
    }

    public Dimension MinWidth
    {
        get => _minWidth;
        set { SetMinWidth(value.Pixels, value.Percent); }
    }

    public Dimension MaxWidth
    {
        get => _maxWidth;
        set { SetMaxWidth(value.Pixels, value.Percent); }
    }

    public Dimension Height
    {
        get => _height;
        set { SetHeight(value.Pixels, value.Percent); }
    }

    public Dimension MinHeight
    {
        get => _minHeight;
        set { SetMinHeight(value.Pixels, value.Percent); }
    }

    public Dimension MaxHeight
    {
        get => _maxHeight;
        set { SetMaxHeight(value.Pixels, value.Percent); }
    }

    #endregion

    /// <summary>
    /// 返回当前元素可提供给子元素的可用空间。
    /// 当轴处于 Fit 模式时，返回 0，表示该轴尺寸由内容反推。
    /// </summary>
    protected Size GetInnerSpace() => new(FitWidth ? 0f : InnerBounds.Width, FitHeight ? 0f : InnerBounds.Height);

    /// <summary>
    /// 获取父容器可用空间；根节点退化为屏幕可用空间。
    /// </summary>
    protected Size GetParentInnerSpace() => Parent?.GetInnerSpace() ?? GraphicsDeviceHelper.GetBackBufferSizeByUIScale();

    public virtual void UpdateLayout()
    {
        if (!LayoutIsDirty) return;

        var availableSize = GetParentInnerSpace();
        Measure(availableSize.Width, availableSize.Height);
        RecalculateHeight();
        CleanupDirtyMark();
    }

    /// <summary>
    /// 测量当前元素尺寸，并将结果写入 Outer/Bounds/Inner 三套盒模型数据。
    /// </summary>
    public virtual void Measure(float width, float height)
    {
        UpdateWidthConstraints(width);
        UpdateHeightConstraints(height);

        if (FitWidth) SetInnerBoundsWidthRaw(WidthMertrics.ClampInner(0f));
        else UpdateBoundsWidth(width);

        if (FitHeight) SetInnerBoundsHeightRaw(HeightMertrics.ClampInner(0f));
        else UpdateBoundsHeight(height);
    }

    /// <summary>
    /// 在父级宽度变化时更新当前宽度。
    /// </summary>
    public virtual void UpdateWidth(float availableWidth)
    {
        UpdateWidthConstraints(availableWidth);

        if (FitWidth) return;

        UpdateBoundsWidth(availableWidth);
    }

    public virtual void RecalculateHeight() { }

    /// <summary>
    /// 在父级高度变化时更新当前高度。
    /// </summary>
    public virtual void UpdateHeight(float availableHeight)
    {
        UpdateHeightConstraints(availableHeight);

        if (FitHeight) return;

        UpdateBoundsHeight(availableHeight);
    }

    /// <summary>
    /// 水平方向约束缓存（保持历史拼写以兼容外部调用）。
    /// </summary>
    public AxisMetrics WidthMertrics;

    /// <summary>
    /// 垂直方向约束缓存（保持历史拼写以兼容外部调用）。
    /// </summary>
    public AxisMetrics HeightMertrics;

    /// <summary>
    /// 更新宽度约束缓存。
    /// </summary>
    /// <param name="availableWidth">当前可用宽度。</param>
    protected void UpdateWidthConstraints(float availableWidth)
    {
        WidthMertrics.UpdateConstraints(
            _minWidth, _maxWidth, availableWidth, BoxSizing,
            Padding.Horizontal, Border, Margin.Horizontal);
    }

    /// <summary>
    /// 更新高度约束缓存。
    /// </summary>
    /// <param name="availableHeight">当前可用高度。</param>
    protected void UpdateHeightConstraints(float availableHeight)
    {
        HeightMertrics.UpdateConstraints(
            _minHeight, _maxHeight, availableHeight, BoxSizing,
            Padding.Vertical, Border, Margin.Vertical);
    }

    /// <summary>
    /// 按声明宽度和约束缓存计算最终宽度。
    /// </summary>
    /// <param name="availableWidth">当前可用宽度。</param>
    protected void UpdateBoundsWidth(float availableWidth)
    {
        WidthMertrics.SetValueClamped(_width.CalculateSize(availableWidth));
        ApplyWidthByBoxSizing(WidthMertrics.Value);
    }

    /// <summary>
    /// 按声明高度和约束缓存计算最终高度。
    /// </summary>
    /// <param name="availableHeight">当前可用高度。</param>
    protected void UpdateBoundsHeight(float availableHeight)
    {
        HeightMertrics.SetValueClamped(_height.CalculateSize(availableHeight));
        ApplyHeightByBoxSizing(HeightMertrics.Value);
    }

    /// <summary>
    /// 根据 BoxSizing 决定宽度值写入 Bounds 还是 InnerBounds。
    /// </summary>
    private void ApplyWidthByBoxSizing(float width)
    {
        switch (BoxSizing)
        {
            default:
            case BoxSizing.Border:
                SetBoundsWidthRaw(width);
                break;
            case BoxSizing.Content:
                SetInnerBoundsWidthRaw(width);
                break;
        }
    }

    /// <summary>
    /// 根据 BoxSizing 决定高度值写入 Bounds 还是 InnerBounds。
    /// </summary>
    private void ApplyHeightByBoxSizing(float height)
    {
        switch (BoxSizing)
        {
            default:
            case BoxSizing.Border:
                SetBoundsHeightRaw(height);
                break;
            case BoxSizing.Content:
                SetInnerBoundsHeightRaw(height);
                break;
        }
    }

    #region 设置 Bounds 的方法，包括 OuterBounds, Bounds, InnerBounds

    /// <summary>
    /// 直接设置 OuterWidth，并同步推导 Bounds/InnerWidth。
    /// </summary>
    internal void SetOuterBoundsWidthRaw(float width)
    {
        OuterBounds.Width = width;
        var boundsWidth = width - Margin.Horizontal;
        Bounds.Width = boundsWidth;
        InnerBounds.Width = boundsWidth - Padding.Horizontal - Border * 2;
    }

    /// <summary>
    /// 直接设置 OuterHeight，并同步推导 Bounds/InnerHeight。
    /// </summary>
    internal void SetOuterBoundsHeightRaw(float height)
    {
        OuterBounds.Height = height;
        var boundsHeight = height - Margin.Vertical;
        Bounds.Height = boundsHeight;
        InnerBounds.Height = boundsHeight - Padding.Vertical - Border * 2;
    }

    /// <summary>
    /// 直接设置 Bounds.Width，并同步推导 Inner/Outer 宽度。
    /// </summary>
    private void SetBoundsWidthRaw(float width)
    {
        Bounds.Width = width;
        InnerBounds.Width = width - Padding.Horizontal - Border * 2;
        OuterBounds.Width = width + Margin.Horizontal;
    }

    /// <summary>
    /// 直接设置 Bounds.Height，并同步推导 Inner/Outer 高度。
    /// </summary>
    private void SetBoundsHeightRaw(float height)
    {
        Bounds.Height = height;
        InnerBounds.Height = height - Padding.Vertical - Border * 2;
        OuterBounds.Height = height + Margin.Vertical;
    }

    /// <summary>
    /// 直接设置 InnerWidth，并同步推导 Bounds/OuterWidth。
    /// </summary>
    public void SetInnerBoundsWidthRaw(float width)
    {
        InnerBounds.Width = width;
        var boundsWidth = width + Padding.Horizontal + Border * 2;
        Bounds.Width = boundsWidth;
        OuterBounds.Width = boundsWidth + Margin.Horizontal;
    }

    /// <summary>
    /// 直接设置 InnerHeight，并同步推导 Bounds/OuterHeight。
    /// </summary>
    public void SetInnerBoundsHeightRaw(float height)
    {
        InnerBounds.Height = height;
        var boundsHeight = height + Padding.Vertical + Border * 2;
        Bounds.Height = boundsHeight;
        OuterBounds.Height = boundsHeight + Margin.Vertical;
    }

    #endregion
}
