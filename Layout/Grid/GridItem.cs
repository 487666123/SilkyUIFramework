namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 布局中的一个子项，绑定实际 UIView 与当前计算出的 Grid 区域。
/// </summary>
public struct GridItem(UIView element, GridArea area)
{
    /// <summary> 参与布局的实际 UI 元素。 </summary>
    public UIView Element { get; } = element;

    /// <summary> 元素在 Grid 中占据的区域。自动放置阶段会更新该值。 </summary>
    public GridArea Area { get; set; } = area;
}
