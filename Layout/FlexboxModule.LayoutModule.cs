using static SilkyUIFramework.Layout.CrossAlignment;

namespace SilkyUIFramework.Layout;

public sealed partial class FlexboxModule(UIElementGroup parent) : LayoutModule(parent)
{
    /// <summary>
    /// 计算换行
    /// </summary>
    public sealed override void MeasureChildren()
    {
        switch (Parent.FlexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                if (Parent.FlexWrap && !Parent.FitWidth) WrapRow();
                else SingleRow();
                break;
            }
            case FlexDirection.Column:
            {
                if (Parent.FlexWrap && !Parent.FitHeight) WrapColumn();
                else SingleColumn();
                break;
            }
        }
    }

    public sealed override void Measure()
    {
        switch (Parent.FlexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                MeasureSize(Parent.Gap.Width, out var mainSize, out var crossSize);
                if (Parent.FitWidth) SetInnerWidthClamped(Parent, mainSize);
                if (Parent.FitHeight) SetInnerHeightClamped(Parent, crossSize);
                break;
            }
            case FlexDirection.Column:
            {
                MeasureSize(Parent.Gap.Height, out var mainSize, out var crossSize);
                if (Parent.FitWidth) SetInnerWidthClamped(Parent, crossSize);
                if (Parent.FitHeight) SetInnerHeightClamped(Parent, mainSize);
                break;
            }
        }
    }

    public sealed override void ResizeChildrenWidth()
    {
        base.ResizeChildrenWidth();

        switch (Parent.FlexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                // 宽度可能被父元素拉伸, 再次计算元素换行
                if (Parent.FlexWrap) WrapRow();
                else
                {
                    foreach (var t in _lines)
                    {
                        t.UpdateMainSizeByRow(Parent.Gap.Width);
                    }
                }

                RowGrowOrShrink();
                break;
            }
            case FlexDirection.Column:
            {
                if (Parent.CrossContentAlignment == CrossContentAlignment.Stretch)
                {
                    var remaining = Parent.InnerBounds.Width - UpdateCrossSize(Parent.Gap.Width);
                    if (remaining > 0)
                    {
                        var share = remaining / _lines.Count;
                        foreach (var t in _lines)
                            t.CrossSize += share;
                    }
                }

                if (Parent.CrossAlignment != Stretch) break;
                foreach (var line in _lines)
                {
                    foreach (var el in line.Elements.Where(el =>
                                 el.Parent.FitWidth || !(el.OuterBounds.Width >= line.CrossSize)))
                    {
                        SetOuterWidthClamped(el, line.CrossSize);
                    }
                }

                break;
            }
        }
    }

    public sealed override void RecalculateHeight()
    {
        if (!Parent.FitHeight) return;

        switch (Parent.FlexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                SetInnerHeightClamped(Parent, UpdateCrossSize(Parent.Gap.Height));
                break;
            }
            case FlexDirection.Column:
            {
                SetInnerHeightClamped(Parent, MaxMainSize());
                break;
            }
        }
    }

    public sealed override void RecalculateChildrenHeight()
    {
        switch (Parent.FlexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                foreach (var line in _lines)
                {
                    line.CrossSize = line.MaxOuterHeight();
                }

                break;
            }
            case FlexDirection.Column:
            {
                foreach (var line in _lines)
                {
                    line.UpdateMainSizeByColumn(Parent.Gap.Height);
                }

                break;
            }
        }
    }

    public sealed override void ResizeChildrenHeight()
    {
        base.ResizeChildrenHeight();

        switch (Parent.FlexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                if (Parent.CrossContentAlignment == CrossContentAlignment.Stretch)
                {
                    var remaining = Parent.InnerBounds.Height - UpdateCrossSize(Parent.Gap.Height);
                    if (remaining > 0)
                    {
                        var share = remaining / _lines.Count;
                        foreach (var line in _lines)
                        {
                            line.CrossSize += share;
                        }
                    }
                }

                if (Parent.CrossAlignment == Stretch)
                {
                    foreach (var line in _lines)
                    {
                        foreach (var el in line.Elements.Where(el =>
                                     el.Parent.FitHeight || !(el.OuterBounds.Height >= line.CrossSize)))
                        {
                            SetOuterHeightClamped(el, line.CrossSize);
                        }
                    }
                }

                var innerBounds = Parent.InnerBounds;

                foreach (var line in _lines)
                    line.UpdateMainAlignment(Parent.MainAlignment, innerBounds.Width, Parent.Gap.Width);

                UpdateCrossContentAlignment(innerBounds.Height, Parent.Gap.Height);
                break;
            }
            case FlexDirection.Column:
            {
                if (Parent.FlexWrap) WrapColumn();
                else
                {
                    foreach (var line in _lines)
                    {
                        line.UpdateMainSizeByColumn(Parent.Gap.Height);
                    }
                }

                ColumnGrowOrShrink();

                var innerBounds = Parent.InnerBounds;

                foreach (var line in _lines)
                    line.UpdateMainAlignment(Parent.MainAlignment, innerBounds.Height, Parent.Gap.Height);

                UpdateCrossContentAlignment(innerBounds.Width, Parent.Gap.Width);
                break;
            }
        }
    }

    private float CrossOffsetCache { get; set; }
    private float CrossGapCache { get; set; }

    public sealed override void ModifyLayoutOffset()
    {
        var crossStart = CrossOffsetCache;

        switch (Parent.FlexDirection)
        {
            case FlexDirection.Row:
            {
                foreach (var line in _lines)
                {
                    var left = line.MainOffset;

                    foreach (var el in line.Elements)
                    {
                        var crossOffset = CalculateCrossOffset(line.CrossSize, el.OuterBounds.Height);
                        el.SetLayoutOffset(left, crossStart + crossOffset);
                        left += el.OuterBounds.Width + line.MainGap;
                    }

                    crossStart += line.CrossSize + CrossGapCache;
                }

                break;
            }
            case FlexDirection.Column:
            {
                foreach (var line in _lines)
                {
                    var top = line.MainOffset;

                    foreach (var el in line.Elements)
                    {
                        var itemCrossOffset = CalculateCrossOffset(line.CrossSize, el.OuterBounds.Width);
                        el.SetLayoutOffset(crossStart + itemCrossOffset, top);
                        top += el.OuterBounds.Height + line.MainGap;
                    }

                    crossStart += line.CrossSize + CrossGapCache;
                }

                break;
            }
            default: goto case FlexDirection.Row;
        }
    }
}