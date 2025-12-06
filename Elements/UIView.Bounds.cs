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
    /// 获取可用空间
    /// </summary>
    protected Size GetInnerSpace() =>
        new(FitWidth ? 0 : InnerBounds.Width, FitHeight ? 0 : InnerBounds.Height);

    /// <summary>
    /// 给自己用的
    /// </summary>
    protected Size GetParentInnerSpace() =>
        Parent?.GetInnerSpace() ?? GraphicsDeviceHelper.GetBackBufferSizeByUIScale();

    public virtual void UpdateLayout()
    {
        if (!LayoutIsDirty) return;

        var availableSize = GetParentInnerSpace();
        PreMeasure(availableSize.Width, availableSize.Height);
        RecalculateHeight();
        CleanupDirtyMark();
    }

    /// <summary>
    /// 预测量元素宽高
    /// </summary>
    public virtual void PreMeasure(float? width, float? height)
    {
        CalculateWidthConstraints(width ?? 0);
        CalculateHeightConstraints(height ?? 0);

        if (FitWidth)
            SetInnerBoundsWidthRaw(MathHelper.Clamp(0f, MinInnerWidth, MaxInnerWidth));
        else
            CalculateBoundsWidth(width ?? 0);

        if (FitHeight)
            SetInnerBoundsHeightRaw(MathHelper.Clamp(0f, MinInnerHeight, MaxInnerHeight));
        else
            CalculateBoundsHeight(height ?? 0);
    }

    public virtual void RefreshWidth(float availableWidth)
    {
        CalculateWidthConstraints(availableWidth);

        if (FitWidth) return;
        CalculateBoundsWidth(availableWidth);
    }

    public virtual void RecalculateHeight() { }

    public virtual void RefreshHeight(float availableHeight)
    {
        CalculateHeightConstraints(availableHeight);

        if (FitHeight) return;
        CalculateBoundsHeight(availableHeight);
    }

    #region 声明和计算约束

    public float MinWidthValue { get; private set; }
    public float MaxWidthValue { get; private set; }
    public float WidthValue { get; private set; }

    public float MinHeightValue { get; private set; }
    public float MaxHeightValue { get; private set; }
    public float HeightValue { get; private set; }

    public float MinOuterWidth { get; private set; }
    public float MaxOuterWidth { get; private set; }
    public float MinOuterHeight { get; private set; }
    public float MaxOuterHeight { get; private set; }

    public float MinInnerWidth { get; private set; }
    public float MaxInnerWidth { get; private set; }
    public float MinInnerHeight { get; private set; }
    public float MaxInnerHeight { get; private set; }

    protected internal void CalculateWidthConstraints(float availableWidth)
    {
        switch (BoxSizing)
        {
            default:
            case BoxSizing.Border:
                MinWidthValue = Math.Max(_minWidth.CalculateSize(availableWidth), Padding.Horizontal + Border * 2);
                MaxWidthValue = Math.Max(_maxWidth.CalculateSize(availableWidth), Padding.Horizontal + Border * 2);
                MinInnerWidth = MinWidthValue - Border * 2 - Padding.Horizontal;
                MaxInnerWidth = MaxWidthValue - Border * 2 - Padding.Horizontal;
                MinOuterWidth = MinWidthValue + Margin.Horizontal;
                MaxOuterWidth = MaxWidthValue + Margin.Horizontal;
                break;
            case BoxSizing.Content:
                MinWidthValue = _minWidth.CalculateSize(availableWidth);
                MaxWidthValue = _maxWidth.CalculateSize(availableWidth);
                MinInnerWidth = MinWidthValue;
                MaxInnerWidth = MaxWidthValue;
                MinOuterWidth = MinWidthValue + Border * 2 + Padding.Horizontal + Margin.Horizontal;
                MaxOuterWidth = MaxWidthValue + Border * 2 + Padding.Horizontal + Margin.Horizontal;
                break;
        }
    }

    protected internal void CalculateHeightConstraints(float availableHeight)
    {
        switch (BoxSizing)
        {
            default:
            case BoxSizing.Border:
                MinHeightValue = Math.Max(_minHeight.CalculateSize(availableHeight), Padding.Vertical + Border * 2);
                MaxHeightValue = Math.Max(_maxHeight.CalculateSize(availableHeight), Padding.Vertical + Border * 2);
                MinInnerHeight = MinHeightValue - Border * 2 - Padding.Vertical;
                MaxInnerHeight = MaxHeightValue - Border * 2 - Padding.Vertical;
                MinOuterHeight = MinHeightValue + Margin.Vertical;
                MaxOuterHeight = MaxHeightValue + Margin.Vertical;
                break;
            case BoxSizing.Content:
                MinHeightValue = _minHeight.CalculateSize(availableHeight);
                MaxHeightValue = _maxHeight.CalculateSize(availableHeight);
                MinInnerHeight = MinHeightValue;
                MaxInnerHeight = MaxHeightValue;
                MinOuterHeight = MinHeightValue + Border * 2 + Padding.Vertical + Margin.Vertical;
                MaxOuterHeight = MaxHeightValue + Border * 2 + Padding.Vertical + Margin.Vertical;
                break;
        }
    }

    #endregion

    /// <summary>
    /// 计算边界宽度<br/>
    /// 根据 <see cref="BoxSizing"/> 决定边界宽度应用于 <see cref="Bounds"/> 还是 <see cref="InnerBounds"/>
    /// </summary>
    /// <param name="availableWidth">父元素可用宽度</param>
    protected void CalculateBoundsWidth(float availableWidth)
    {
        WidthValue = _width.CalculateSize(availableWidth);
        WidthValue = MathHelper.Clamp(WidthValue, MinWidthValue, MaxWidthValue);

        switch (BoxSizing)
        {
            default:
            case BoxSizing.Border:
                SetBoundsWidthRaw(WidthValue);
                break;
            case BoxSizing.Content:
                SetInnerBoundsWidthRaw(WidthValue);
                break;
        }
    }

    /// <summary>
    /// 计算边界高度<br/>
    /// 根据 <see cref="BoxSizing"/> 决定边界高度应用于 <see cref="Bounds"/> 还是 <see cref="InnerBounds"/>
    /// </summary>
    /// <param name="availableHeight">父元素可用宽度</param>
    protected void CalculateBoundsHeight(float availableHeight)
    {
        HeightValue = _height.CalculateSize(availableHeight);
        HeightValue = MathHelper.Clamp(HeightValue, MinHeightValue, MaxHeightValue);

        switch (BoxSizing)
        {
            default:
            case BoxSizing.Border:
                SetBoundsHeightRaw(HeightValue);
                break;
            case BoxSizing.Content:
                SetInnerBoundsHeightRaw(HeightValue);
                break;
        }
    }

    #region 设置 Bounds 的方法，包括 OuterBounds, Bounds, InnerBounds

    internal void SetOuterBoundsWidthRaw(float width)
    {
        OuterBounds.Width = width;
        Bounds.Width = width -= Margin.Horizontal;
        InnerBounds.Width = width - Padding.Horizontal - Border * 2;
    }

    internal void SetOuterBoundsHeightRaw(float height)
    {
        OuterBounds.Height = height;
        Bounds.Height = height -= Margin.Vertical;
        InnerBounds.Height = height - Padding.Vertical - Border * 2;
    }

    private void SetBoundsWidthRaw(float width)
    {
        Bounds.Width = width;
        InnerBounds.Width = width - Padding.Horizontal - Border * 2;
        OuterBounds.Width = width + Margin.Horizontal;
    }

    private void SetBoundsHeightRaw(float height)
    {
        Bounds.Height = height;
        InnerBounds.Height = height - Padding.Vertical - Border * 2;
        OuterBounds.Height = height + Margin.Vertical;
    }

    public void SetInnerBoundsWidthRaw(float width)
    {
        InnerBounds.Width = width;
        Bounds.Width = width += Padding.Horizontal + Border * 2;
        OuterBounds.Width = width + Margin.Horizontal;
    }

    public void SetInnerBoundsHeightRaw(float height)
    {
        InnerBounds.Height = height;
        Bounds.Height = height += Padding.Vertical + Border * 2;
        OuterBounds.Height = height + Margin.Vertical;
    }

    #endregion
}