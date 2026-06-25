namespace SilkyUIFramework.Layout;

/// <summary>
/// Grid 自动放置方向。
/// </summary>
public enum GridFlowDirection
{
    /// <summary> 优先沿列方向向右推进，当前行放满后进入下一行。 </summary>
    Row,

    /// <summary> 优先沿行方向向下推进，当前列放满后进入下一列。 </summary>
    Column
}

/// <summary>
/// Grid 轨道尺寸类型。
/// </summary>
public enum TemplateType
{
    /// <summary> 根据内容外部尺寸撑开。 </summary>
    Auto,

    /// <summary> 按剩余空间比例分配，对应 CSS Grid 的 fr。 </summary>
    Fraction,

    /// <summary> 固定像素尺寸。 </summary>
    Pixels,

    /// <summary> 按父容器对应轴 InnerBounds 的百分比计算。 </summary>
    Percent
}

/// <summary>
/// Grid 子项在其 Grid 区域内的单轴对齐方式。
/// </summary>
public enum GridItemAlignment
{
    /// <summary> 使用父 Grid 容器在对应轴上的默认子项对齐方式。 </summary>
    Inherit,

    /// <summary> 靠近 Grid 区域起点。 </summary>
    Start,

    /// <summary> 在 Grid 区域内居中。 </summary>
    Center,

    /// <summary> 靠近 Grid 区域终点。 </summary>
    End,

    /// <summary> 拉伸到填满 Grid 区域。 </summary>
    Stretch
}
