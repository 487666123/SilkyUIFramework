using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

public partial class UIElementGroup
{
    #region Properties & Fields LayoutType LayoutDirection Gap

    /// <summary>
    /// 目前仅有 Flexbox 可以使用，请不要自定义布局。
    /// </summary>
    public LayoutType LayoutType
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            MarkLayoutDirty();
        }
    } = LayoutType.Flexbox;

    public Size Gap
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public void SetGap(float gap) => Gap = gap;
    public void SetGap(float width, float height) => Gap = Gap.With(width, height);

    private readonly FlexboxModule FlexboxModule;
    private readonly GridModule GridModule;

    public LayoutModule LayoutModule
    {
        get
        {
            return LayoutType switch
            {
                LayoutType.Grid => GridModule,
                LayoutType.Custom => field,
                { } => FlexboxModule,
            };
        }
        set
        {
            if (value.Parent != this) return;
            if (field?.GetType() == value?.GetType()) return;
            field = value;
            if (LayoutType == LayoutType.Custom) MarkLayoutDirty();
        }
    }

    #endregion

    public override void Measure(float width, float height)
    {
        base.Measure(width, height);

        MeasureChildren();

        if (LayoutElements.Count <= 0) return;
        LayoutModule?.Measure();
    }

    /// <summary>
    /// 测量子元素宽高
    /// </summary>
    public virtual void MeasureChildren()
    {
        ClassifyChildren();
        if (LayoutElements.Count <= 0) return;

        LayoutModule?.PrepareData();

        var availableWidth = FitWidth ? 0 : InnerBounds.Width;
        var availableHeight = FitHeight ? 0 : InnerBounds.Height;

        for (var i = 0; i < LayoutElements.Count; i++)
        {
            var cacheWidth = availableWidth;
            var cacheHeight = availableHeight;
            LayoutElements[i].Measure(cacheWidth, cacheHeight);
        }

        LayoutModule?.MeasureChildren();
    }

    /// <summary> 重设宽度 </summary>
    public virtual void ResizeChildrenWidth()
    {
        if (LayoutElements.Count <= 0) return;

        LayoutModule?.ResizeChildrenWidth();

        foreach (var item in LayoutElements.OfType<UIElementGroup>())
        {
            item.ResizeChildrenWidth();
        }
    }

    /// <summary>
    /// 设定宽度之后重新计算高度, 适应某些因宽度而改变高度的情况
    /// </summary>
    public override void RecalculateHeight()
    {
        base.RecalculateHeight();

        RecalculateChildrenHeight();

        LayoutModule?.RecalculateHeight();
    }

    protected virtual void RecalculateChildrenHeight()
    {
        if (LayoutElements.Count <= 0) return;

        foreach (var el in LayoutElements)
        {
            el.RecalculateHeight();
        }

        LayoutModule?.RecalculateChildrenHeight();
    }

    protected virtual void ResizeChildrenHeight()
    {
        if (LayoutElements.Count <= 0) return;

        LayoutModule?.ResizeChildrenHeight();

        foreach (var item in LayoutElements.OfType<UIElementGroup>())
        {
            item.ResizeChildrenHeight();
        }
    }


    protected virtual void UpdateChildrenLayoutOffset()
    {
        if (LayoutElements.Count <= 0) return;

        LayoutModule.UpdateChildrenLayoutOffset();

        foreach (var child in LayoutElements.OfType<UIElementGroup>())
        {
            child.UpdateChildrenLayoutOffset();
        }
    }
}