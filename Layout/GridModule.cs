using SilkyUIFramework.Layout.Grid;

namespace SilkyUIFramework.Layout;

/// <summary>
/// Grid 布局模块，负责在现有 <see cref="LayoutModule"/> 生命周期中完成子项放置、轨道尺寸解析和位置更新。
/// </summary>
public sealed class GridModule(UIElementGroup container) : LayoutModule(container)
{
    private readonly GridContext _context = new(container);

    /// <summary>
    /// 初始化本轮 Grid 计算所需的临时状态，并先完成子项所在网格区域的放置。
    /// 此时子项已经被父级粗测过一次，可以用其 OuterBounds 参与后续 Auto 轨道计算。
    /// </summary>
    public override void PrepareData()
    {
        _context.Initialize();
        GridPlacement.PlaceItems(_context);
    }

    /// <summary>
    /// 子项完成初始测量后，先解析列宽。
    /// 后续 ResizeChildrenWidth 会根据列宽把子项拉伸到所在 Grid 区域。
    /// </summary>
    public override void MeasureChildren()
    {
        GridTrackSizing.ResolveColumns(_context);
    }

    /// <summary>
    /// 解析行高，并在父容器 FitWidth/FitHeight 时用 Grid 内容尺寸反推容器尺寸。
    /// 容器尺寸变化后，对应方向需要重新解析一次轨道，保证百分比与 fr 能基于最新空间计算。
    /// </summary>
    public override void Measure()
    {
        GridTrackSizing.ResolveRows(_context);

        if (Container.FitWidth)
        {
            Container.SetInnerWidthClamped(_context.TotalColumnsWidth);
            GridTrackSizing.ResolveColumns(_context);
        }

        if (Container.FitHeight)
        {
            Container.SetInnerHeightClamped(_context.TotalRowsHeight);
            GridTrackSizing.ResolveRows(_context);
        }
    }

    /// <summary>
    /// 根据已解析的列宽，把每个子项宽度设置为其覆盖的 Grid 区域宽度。
    /// </summary>
    public override void ResizeChildrenWidth()
    {
        GridTrackSizing.ResolveColumns(_context);

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
    }

    /// <summary>
    /// 根据已解析的行高，把每个子项高度设置为其覆盖的 Grid 区域高度。
    /// </summary>
    public override void ResizeChildrenHeight()
    {
        GridTrackSizing.ResolveRows(_context);

        foreach (var item in _context.Items)
        {
            var areaHeight = _context.GetAreaHeight(item.Area);
            if (ResolveVerticalAlignment(item.Element) == GridItemAlignment.Stretch)
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
    /// 将轨道尺寸转换为每条轨道的偏移量，并把子项移动到对应 Grid 区域左上角。
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
