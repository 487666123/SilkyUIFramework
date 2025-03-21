﻿namespace SilkyUIFramework.BasicElements;

public partial class View
{
    #region Flexbox 属性

    /// <summary> 是否换行 </summary>
    public bool FlexWrap { get; set; } = false;

    /// <summary>
    /// 填充剩余空间 (最后计算)
    /// </summary>
    public (bool Enable, float Value) FlexWeight = (false, 1f);

    /// <summary> 主轴对齐 </summary>
    public MainAlignment MainAlignment { get; set; } = MainAlignment.Start;

    /// <summary> 交叉轴对齐 </summary>
    public CrossAlignment CrossAlignment { get; set; } = CrossAlignment.Start;

    #endregion

    public Vector2 CalculateContentSizeByFlexbox()
    {
        if (ElementGroups is null || ElementGroups.Count == 0)
            return Vector2.Zero;

        return LayoutDirection switch
        {
            LayoutDirection.Column => new Vector2(ElementGroups.Sum(item => item.Width + Gap.X) - Gap.X,
                                ElementGroups.Max(item => item.Height)),
            LayoutDirection.Row => new Vector2(ElementGroups.Max(item => item.Width),
                                ElementGroups.Sum(item => item.Height + Gap.Y) - Gap.Y),
            _ => Vector2.Zero,
        };
    }

    protected void SortElementGroupsByFlexbox()
    {
        // 不换行
        if (!FlexWrap || (LayoutDirection is LayoutDirection.Row && !SpecifyWidth) ||
            (LayoutDirection is LayoutDirection.Column && !SpecifyHeight))
        {
            switch (LayoutDirection)
            {
                default:
                case LayoutDirection.Row:
                {
                    ArrangeDirection = LayoutDirection.Row;
                    var elementGroups = new ElementGroup(this);
                    ElementGroups.Add(elementGroups);
                    for (var i = 0; i < FlowElements.Count; i++)
                    {
                        elementGroups.Append(FlowElements[i]);
                    }

                    return;
                }
                case LayoutDirection.Column:
                {
                    ArrangeDirection = LayoutDirection.Column;
                    var elementGroups = new ElementGroup(this);
                    ElementGroups.Add(elementGroups);
                    for (var i = 0; i < FlowElements.Count; i++)
                    {
                        elementGroups.Append(FlowElements[i]);
                    }

                    return;
                }
            }
        }

        // 换行
        switch (LayoutDirection)
        {
            default:
            case LayoutDirection.Row:
            {
                ArrangeDirection = LayoutDirection.Row;
                var elementGroups = new ElementGroup(this);
                ElementGroups.Add(elementGroups);
                var maxWidth = SpecifyWidth ? _innerDimensions.Width : float.MaxValue;

                for (var i = 0; i < FlowElements.Count; i++)
                {
                    if (!elementGroups.IsEnoughSpace(FlowElements[i], maxWidth))
                    {
                        ElementGroups.Add(elementGroups = new ElementGroup(this));
                    }

                    elementGroups.Append(FlowElements[i]);
                }

                break;
            }
            case LayoutDirection.Column:
            {
                ArrangeDirection = LayoutDirection.Column;
                var elementGroups = new ElementGroup(this);
                ElementGroups.Add(elementGroups);
                var maxHeight = SpecifyHeight ? _innerDimensions.Height : float.MaxValue;

                for (var i = 0; i < FlowElements.Count; i++)
                {
                    if (!elementGroups.IsEnoughSpace(FlowElements[i], maxHeight))
                    {
                        ElementGroups.Add(elementGroups = new ElementGroup(this));
                    }

                    elementGroups.Append(FlowElements[i]);
                }

                break;
            }
        }
    }

    protected void StretchFlexItemGroupWidth()
    {
        if (ElementGroups.Count == 1 && ElementGroups[0] != null)
        {
            if (SpecifyWidth && _innerDimensions.Width > ElementGroups[0].Width)
                ElementGroups[0].Width = _innerDimensions.Width;
        }
    }

    protected void StretchFlexItemGroupHeight()
    {
        if (ElementGroups.Count == 1 && ElementGroups[0] != null)
        {
            if (SpecifyWidth && _innerDimensions.Height > ElementGroups[0].Height)
                ElementGroups[0].Height = _innerDimensions.Height;
        }
    }

    protected virtual void ArrangeElementsByFlexbox()
    {
        switch (LayoutDirection)
        {
            default:
            case LayoutDirection.Row:
                StretchFlexItemGroupHeight();
                if (!SpecifyWidth)
                {
                    var top = 0f;
                    StretchFlexItemGroupHeight();
                    foreach (var elementGroup in ElementGroups)
                    {
                        elementGroup.Arrange(0f, top);
                        top += elementGroup.Height + Gap.Y;
                    }
                }
                else
                {
                    switch (MainAlignment)
                    {
                        default:
                        case MainAlignment.Start:
                        {
                            var top = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                elementGroup.Arrange(0f, top);
                                top += elementGroup.Height + Gap.Y;
                            }

                            break;
                        }
                        case MainAlignment.End:
                        {
                            var top = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                elementGroup.Arrange(_innerDimensions.Width - elementGroup.Width, top);
                                top += elementGroup.Height + Gap.Y;
                            }

                            break;
                        }
                        case MainAlignment.Center:
                        {
                            var top = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                elementGroup.Arrange((_innerDimensions.Width - elementGroup.Width) / 2f, top);
                                top += elementGroup.Height + Gap.Y;
                            }

                            break;
                        }
                        case MainAlignment.SpaceEvenly:
                        {
                            var top = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                var sum = elementGroup.Children.Sum(el => el.Width);
                                var gap = (_innerDimensions.Width - sum) / (elementGroup.Children.Count + 1);

                                elementGroup.Arrange(gap, top, gap);
                                top += elementGroup.Height + Gap.Y;
                            }

                            break;
                        }
                        case MainAlignment.SpaceBetween:
                        {
                            var top = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                var sum = elementGroup.Children.Sum(el => el.Width);
                                var gap = (_innerDimensions.Width - sum) / (elementGroup.Children.Count - 1);

                                elementGroup.Arrange(0f, top, gap);
                                top += elementGroup.Height + Gap.Y;
                            }

                            break;
                        }
                    }
                }

                break;
            case LayoutDirection.Column:
                StretchFlexItemGroupWidth();
                if (!SpecifyHeight)
                {
                    var left = 0f;
                    foreach (var elementGroup in ElementGroups)
                    {
                        elementGroup.Arrange(left, 0f);
                        left += elementGroup.Width + Gap.X;
                    }
                }
                else
                {
                    switch (MainAlignment)
                    {
                        default:
                        case MainAlignment.Start:
                        {
                            var left = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                elementGroup.Arrange(left, 0f);
                                left += elementGroup.Width + Gap.X;
                            }

                            break;
                        }
                        case MainAlignment.End:
                        {
                            var left = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                elementGroup.Arrange(left, _innerDimensions.Height - elementGroup.Height);
                                left += elementGroup.Width + Gap.X;
                            }

                            break;
                        }
                        case MainAlignment.Center:
                        {
                            var left = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                elementGroup.Arrange(left, (_innerDimensions.Height - elementGroup.Height) / 2f);
                                left += elementGroup.Width + Gap.X;
                            }

                            break;
                        }
                        case MainAlignment.SpaceEvenly:
                        {
                            var left = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                var sum = elementGroup.Children.Sum(el => el.Height);
                                var gap = (_innerDimensions.Height - sum) / (elementGroup.Children.Count + 1);

                                elementGroup.Arrange(left, gap, gap);
                                left += elementGroup.Width + Gap.X;
                            }

                            break;
                        }
                        case MainAlignment.SpaceBetween:
                        {
                            var left = 0f;
                            foreach (var elementGroup in ElementGroups)
                            {
                                var sum = elementGroup.Children.Sum(el => el.Height);
                                var gap = (_innerDimensions.Height - sum) / (elementGroup.Children.Count - 1);

                                elementGroup.Arrange(left, 0f, gap);
                                left += elementGroup.Width + Gap.X;
                            }

                            break;
                        }
                    }
                }

                break;
        }
    }
}