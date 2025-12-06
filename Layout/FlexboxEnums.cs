namespace SilkyUIFramework.Layout;

/// <summary> Flexbox 布局方向 </summary>
public enum FlexDirection
{
    Row,
    Column
}

/// <summary> 主轴对其方式 </summary>
public enum MainAlignment
{
    /// <summary> 总体靠左 </summary>
    Start,

    /// <summary> 总体靠右 </summary>
    End,

    /// <summary> 总体居中 </summary>
    Center,

    /// <summary> 平分空间 </summary>
    SpaceEvenly,

    /// <summary> 两端对齐 </summary>
    SpaceBetween,
}

/// <summary> 交叉轴对齐方式 </summary>
public enum CrossAlignment
{
    /// <summary> 总体靠上 </summary>
    Start,

    /// <summary> 总体居中 </summary>
    Center,

    /// <summary> 总体靠下 </summary>
    End,

    /// <summary> 拉伸 </summary>
    Stretch,
}

public enum CrossContentAlignment
{
    Start,
    Center,
    End,
    SpaceEvenly,
    SpaceBetween,
    Stretch,
}