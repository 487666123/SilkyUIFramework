using static SilkyUIFramework.Layout.CrossAlignment;

namespace SilkyUIFramework.Layout;

public partial class FlexboxModule(UIElementGroup parent) : LayoutModule(parent)
{
    private float _crossOffsetCache, _crossGapCache;

    public sealed override void PreMeasure()
    {
        // 测量尺寸 + 设定宽高
        switch (_flexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                MeasureSize(Gap.Width, out var mainSize, out var crossSize);
                if (FitWidth) SetInnerWidthClamped(Parent, mainSize);
                if (FitHeight) SetInnerHeightClamped(Parent, crossSize);
                break;
            }
            case FlexDirection.Column:
            {
                MeasureSize(Gap.Height, out var mainSize, out var crossSize);
                if (FitWidth) SetInnerWidthClamped(Parent, crossSize);
                if (FitHeight) SetInnerHeightClamped(Parent, mainSize);
                break;
            }
        }
    }

    public sealed override void PreMeasureChildren()
    {
        // 确定换行与方向
        switch (_flexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                if (_flexWrap && !FitWidth) WrapRow();
                else SingleRow();
                break;
            }
            case FlexDirection.Column:
            {
                if (_flexWrap && !FitHeight) WrapColumn();
                else SingleColumn();
                break;
            }
        }
    }

    public sealed override void ResizeChildrenWidth()
    {
        switch (_flexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                // 宽度可能被父元素拉伸, 再次计算元素换行
                if (_flexWrap) WrapRow();
                else
                {
                    foreach (var t in _flexLines)
                    {
                        t.UpdateMainSizeByRow(Gap.Width);
                    }
                }

                RowGrowOrShrink();
                break;
            }
            case FlexDirection.Column:
            {
                if (_crossContentAlignment == CrossContentAlignment.Stretch)
                {
                    var remaining = Parent.InnerBounds.Width - UpdateCrossSize(Gap.Width);
                    if (remaining > 0)
                    {
                        var share = remaining / _flexLines.Count;
                        foreach (var t in _flexLines)
                            t.CrossSize += share;
                    }
                }

                if (_crossAlignment != Stretch) break;
                foreach (var line in _flexLines)
                {
                    foreach (var el in line.Elements.Where(el =>
                                 el.FitWidth || !(el.OuterBounds.Width >= line.CrossSize)))
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
        if (!FitHeight) return;
        switch (_flexDirection)
        {
            default:
            case FlexDirection.Row:
                SetInnerHeightClamped(Parent, UpdateCrossSize(Gap.Height));
                break;
            case FlexDirection.Column:
                SetInnerHeightClamped(Parent, MaxMainSize());
                break;
        }
    }

    public sealed override void RecalculateChildrenHeight()
    {
        switch (_flexDirection)
        {
            default:
            case FlexDirection.Row:
                foreach (var line in _flexLines)
                {
                    line.CrossSize = line.MaxOuterHeight();
                }

                break;
            case FlexDirection.Column:
                foreach (var line in _flexLines)
                {
                    line.UpdateMainSizeByColumn(Gap.Height);
                }

                break;
        }
    }

    public sealed override void ResizeChildrenHeight()
    {
        switch (_flexDirection)
        {
            default:
            case FlexDirection.Row:
            {
                if (_crossContentAlignment == CrossContentAlignment.Stretch)
                {
                    var remaining = Parent.InnerBounds.Height - UpdateCrossSize(Gap.Height);
                    if (remaining > 0)
                    {
                        var share = remaining / _flexLines.Count;
                        foreach (var line in _flexLines)
                        {
                            line.CrossSize += share;
                        }
                    }
                }

                if (_crossAlignment == Stretch)
                {
                    foreach (var line in _flexLines)
                    {
                        foreach (var el in line.Elements.Where(el =>
                                     el.FitHeight || !(el.OuterBounds.Height >= line.CrossSize)))
                        {
                            SetOuterHeightClamped(el, line.CrossSize);
                        }
                    }
                }

                var innerBounds = Parent.InnerBounds;
                var gap = Gap;

                foreach (var line in _flexLines)
                    line.UpdateMainAlignment(_mainAlignment, innerBounds.Width, gap.Width);

                UpdateCrossContentAlignment(innerBounds.Height, gap.Height);
                break;
            }
            case FlexDirection.Column:
            {
                if (_flexWrap) WrapColumn();
                else
                {
                    foreach (var line in _flexLines)
                    {
                        line.UpdateMainSizeByColumn(Gap.Height);
                    }
                }

                ColumnGrowOrShrink();

                var innerBounds = Parent.InnerBounds;
                var gap = Gap;

                foreach (var line in _flexLines)
                    line.UpdateMainAlignment(_mainAlignment, innerBounds.Height, gap.Height);

                UpdateCrossContentAlignment(innerBounds.Width, gap.Width);
                break;
            }
        }
    }

    public sealed override void ModifyLayoutOffset()
    {
        var crossStart = _crossOffsetCache;
        var crossGap = _crossGapCache;

        switch (_flexDirection)
        {
            case FlexDirection.Row:
            {
                foreach (var line in _flexLines)
                {
                    var left = line.MainOffset;

                    foreach (var el in line.Elements)
                    {
                        var crossOffset = CalculateCrossOffset(line.CrossSize, el.OuterBounds.Height);
                        el.SetLayoutOffset(left, crossStart + crossOffset);
                        left += el.OuterBounds.Width + line.MainGap;
                    }

                    crossStart += line.CrossSize + crossGap;
                }

                break;
            }
            case FlexDirection.Column:
            {
                foreach (var line in _flexLines)
                {
                    var top = line.MainOffset;

                    foreach (var el in line.Elements)
                    {
                        var itemCrossOffset = CalculateCrossOffset(line.CrossSize, el.OuterBounds.Width);
                        el.SetLayoutOffset(crossStart + itemCrossOffset, top);
                        top += el.OuterBounds.Height + line.MainGap;
                    }

                    crossStart += line.CrossSize + crossGap;
                }

                break;
            }
            default: goto case FlexDirection.Row;
        }
    }
}