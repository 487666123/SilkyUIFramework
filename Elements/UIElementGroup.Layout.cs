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

    private readonly Layout.FlexboxModule FlexboxModule;
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

    public override void PreMeasure(float? width, float? height)
    {
        base.PreMeasure(width, height);

        PreMeasureChildren();

        if (LayoutElements.Count <= 0) return;
        LayoutModule?.PreMeasure();
    }

    /// <summary>
    /// 预测量子元素宽高
    /// </summary>
    public virtual void PreMeasureChildren()
    {
        ClassifyChildren();
        if (LayoutElements.Count <= 0) return;

        // 有子元素时，后续需要布局计算，所以同步缓存
        LayoutModule?.UpdateCacheStatus();

        float? availableWidth = FitWidth ? null : InnerBounds.Width;
        float? availableHeight = FitHeight ? null : InnerBounds.Height;

        for (var i = 0; i < LayoutElements.Count; i++)
        {
            var cacheWidth = availableWidth;
            var cacheHeight = availableHeight;
            LayoutModule?.ModifyAvailableSize(LayoutElements[i], i, ref cacheWidth, ref cacheHeight);
            LayoutElements[i].PreMeasure(cacheWidth, cacheHeight);
        }

        LayoutModule?.PreMeasureChildren();
    }

    /// <summary> 重设宽度 </summary>
    public virtual void ResizeChildrenWidth()
    {
        if (LayoutElements.Count <= 0) return;

        if (!FitWidth)
        {
            foreach (var element in LayoutElements)
            {
                element.RefreshWidth(InnerBounds.Width);
            }
        }

        LayoutModule?.ResizeChildrenWidth();

        foreach (var item in LayoutElements.OfType<UIElementGroup>())
        {
            item.ResizeChildrenWidth();
        }
    }

    public override void RecalculateHeight()
    {
        base.RecalculateHeight();
        RecalculateChildrenHeight();

        LayoutModule?.RecalculateHeight();
    }

    protected virtual void RecalculateChildrenHeight()
    {
        if (LayoutElements.Count <= 0) return;
        foreach (var element in LayoutElements)
        {
            element.RecalculateHeight();
        }

        LayoutModule?.RecalculateChildrenHeight();
    }

    protected virtual void ResizeChildrenHeight()
    {
        if (LayoutElements.Count <= 0) return;
        var innerSize = InnerBounds.Size;

        if (!FitHeight)
        {
            foreach (var element in LayoutElements)
            {
                element.RefreshHeight(innerSize.Height);
            }
        }

        LayoutModule?.ResizeChildrenHeight();

        foreach (var item in LayoutElements.OfType<UIElementGroup>())
        {
            item.ResizeChildrenHeight();
        }
    }


    protected virtual void UpdateChildrenLayoutOffset()
    {
        if (LayoutElements.Count <= 0) return;

        LayoutModule.ModifyLayoutOffset();

        foreach (var child in LayoutElements.OfType<UIElementGroup>())
        {
            child.UpdateChildrenLayoutOffset();
        }
    }
}