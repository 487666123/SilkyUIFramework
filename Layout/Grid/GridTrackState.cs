namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// 单条 Grid 轨道在一次布局计算中的可变状态。
/// </summary>
public struct GridTrackState(GridTrack definition)
{
    /// <summary> 轨道的声明定义。 </summary>
    public GridTrack Definition { get; } = definition;

    /// <summary> 固定尺寸或内容贡献得到的基础尺寸。 </summary>
    public float BaseSize { get; set; }

    /// <summary> 经过 fr 分配等步骤后的最终尺寸。 </summary>
    public float FinalSize { get; set; }

    /// <summary> 轨道相对父容器 InnerBounds 的起始偏移。 </summary>
    public float Offset { get; set; }
}
