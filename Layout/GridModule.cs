using SilkyUIFramework.Layout.Grid;

namespace SilkyUIFramework.Layout;

/// <summary>
/// Grid 布局模块，负责在现有 <see cref="LayoutModule"/> 生命周期中完成子项放置、轨道尺寸解析和位置更新。
/// </summary>
public sealed class GridModule(UIElementGroup container) : LayoutModule(container)
{
    private readonly GridContext _context = new(container);
    private float _lastResolvedColumnsWidth = -1f;
    private float _lastResolvedRowsHeight = -1f;

    /// <summary>
    /// 初始化本轮 Grid 计算所需的临时状态，并先完成子项所在网格区域的放置。
    /// 此时本轮子项测量尚未执行；Auto 轨道会在子项测量后使用其 OuterBounds。
    /// </summary>
    public override void PrepareData()
    {
        _context.Initialize();
        GridPlacement.PlaceItems(_context);
    }

    /// <summary>
    /// 子项完成初始测量后，先解析列宽。
    /// 后续 ResizeChildrenWidth 会根据列宽和水平对齐方式更新子项宽度。
    /// </summary>
    public override void MeasureChildren()
    {
        GridTrackSizing.ResolveColumns(_context);
        _lastResolvedColumnsWidth = Container.InnerBounds.Width;
    }

    /// <summary>
    /// 在父容器 FitWidth 时用当前列总宽度反推容器宽度。
    /// 容器宽度变化后重新解析列轨道，保证百分比与 fr 基于最新宽度计算。
    /// </summary>
    public override void Measure()
    {
        if (!Container.FitWidth) return;

        Container.SetInnerWidthClamped(_context.TotalColumnsWidth);
        GridTrackSizing.ResolveColumns(_context);
        _lastResolvedColumnsWidth = Container.InnerBounds.Width;
    }

    /// <summary>
    /// 子项完成宽度变化后的高度重算后，若容器需要按内容收缩高度，则同步回写容器高度。
    /// 行轨道已经统计宽度变化后的内容及比例高度，容器不再使用初始粗测结果。
    /// </summary>
    public override void RecalculateHeight()
    {
        if (!Container.FitHeightToContent) return;

        Container.SetInnerHeightClamped(_context.TotalRowsHeight);
    }

    /// <summary>
    /// 根据已解析的列宽更新每个子项的宽度。
    /// Stretch 直接设置外部宽度；其他对齐方式调用 UpdateWidth 更新可用宽度。
    /// </summary>
    public override void ResizeChildrenWidth()
    {
        var currentWidth = Container.InnerBounds.Width;
        if (Math.Abs(currentWidth - _lastResolvedColumnsWidth) > 0.001f)
        {
            GridTrackSizing.ResolveColumns(_context);
            _lastResolvedColumnsWidth = currentWidth;
        }

        foreach (var item in _context.Items)
        {
            var areaWidth = _context.GetAreaWidth(item.Area);
            if (ResolveHorizontalAlignment(item.Element) == GridItemAlignment.Stretch)
            {
                item.Element.SetOuterWidthClamped(areaWidth);
            }
            else
            {
                item.Element.UpdateWidth(areaWidth);
            }
        }
    }

    /// <summary>
    /// 子项因宽度变化完成高度重算后，重新解析行高。
    /// 这一步用于支持文本等“宽度影响高度”的元素。
    /// </summary>
    public override void RecalculateChildrenHeight()
    {
        GridTrackSizing.ResolveRows(_context);
        _lastResolvedRowsHeight = Container.InnerBounds.Height;
    }

    /// <summary>
    /// 根据已解析的行高更新每个子项的高度。
    /// 非比例子项的 Stretch 直接设置外部高度；比例子项保留按宽度和高度约束计算的高度。
    /// </summary>
    public override void ResizeChildrenHeight()
    {
        var currentHeight = Container.InnerBounds.Height;
        if (Math.Abs(currentHeight - _lastResolvedRowsHeight) > 0.001f)
        {
            // 父级分配可能改变容器高度；先刷新 Auto 行的子树贡献，再解析行。
            var availableHeight = Container.FitHeightToContent ? 0f : currentHeight;
            foreach (var item in _context.Items)
            {
                if (!_context.HasContentSizedRows(item.Area)) continue;
                if (Container.FitHeightToContent && !item.Element.UsesAspectRatio) continue;

                item.Element.UpdateHeight(availableHeight);
                item.Element.RecalculateHeight();
                item.Element.ApplyAspectRatioHeight();
            }

            GridTrackSizing.ResolveRows(_context);
            _lastResolvedRowsHeight = currentHeight;
        }

        foreach (var item in _context.Items)
        {
            var areaHeight = _context.GetAreaHeight(item.Area);
            if (item.Element.UsesAspectRatio && _context.HasContentSizedRows(item.Area))
            {
                // Auto 行已按容器测量基准（内容自适应时为 0）统计比例高度；
                // 不再改用区域高度解析百分比约束，避免子项与行高循环依赖。
                item.Element.ApplyAspectRatioHeight();
            }
            else if (!item.Element.UsesAspectRatio &&
                     ResolveVerticalAlignment(item.Element) == GridItemAlignment.Stretch)
            {
                item.Element.SetOuterHeightClamped(areaHeight);
            }
            else
            {
                item.Element.UpdateHeight(areaHeight);
            }
        }
    }

    /// <summary>
    /// 将轨道尺寸转换为每条轨道的偏移量，并把子项移动到对应 Grid 区域内的对齐位置。
    /// </summary>
    public override void UpdateChildrenLayoutPosition()
    {
        GridTrackSizing.UpdateOffsets(_context);

        foreach (var item in _context.Items)
        {
            var areaWidth = _context.GetAreaWidth(item.Area);
            var areaHeight = _context.GetAreaHeight(item.Area);
            var x = _context.Columns[item.Area.Column].Offset +
                    CalculateAlignmentOffset(areaWidth, item.Element.OuterBounds.Width, ResolveHorizontalAlignment(item.Element));
            var y = _context.Rows[item.Area.Row].Offset +
                    CalculateAlignmentOffset(areaHeight, item.Element.OuterBounds.Height, ResolveVerticalAlignment(item.Element));
            item.Element.SetLayoutOffset(x, y);
        }
    }

    private GridItemAlignment ResolveHorizontalAlignment(UIView element)
    {
        return ResolveAlignment(element.GridHorizontalAlignment, Container.GridItemsHorizontalAlignment);
    }

    private GridItemAlignment ResolveVerticalAlignment(UIView element)
    {
        return ResolveAlignment(element.GridVerticalAlignment, Container.GridItemsVerticalAlignment);
    }

    private static GridItemAlignment ResolveAlignment(GridItemAlignment selfAlignment, GridItemAlignment parentAlignment)
    {
        var resolved = selfAlignment == GridItemAlignment.Inherit ? parentAlignment : selfAlignment;
        return resolved == GridItemAlignment.Inherit ? GridItemAlignment.Stretch : resolved;
    }

    private static float CalculateAlignmentOffset(float areaSize, float itemSize, GridItemAlignment alignment)
    {
        return alignment switch
        {
            GridItemAlignment.Center => (areaSize - itemSize) / 2f,
            GridItemAlignment.End => areaSize - itemSize,
            _ => 0f
        };
    }
}
