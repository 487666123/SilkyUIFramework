namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// 单条 Grid 轨道在一次布局计算中的可变状态。
/// </summary>
public struct GridTrackOutput(GridTrack definition)
{
    /// <summary> 轨道的声明定义。 </summary>
    public GridTrack Definition { get; } = definition;

    /// <summary> 经过固定、内容撑开和 fr 分配等步骤后的最终尺寸。 </summary>
    public float Size { get; set; }

    /// <summary> 本轮布局解析出的最小尺寸。 </summary>
    public float MinSize { get; set; }

    /// <summary> 本轮布局解析出的最大尺寸。正无穷表示没有固定上限。 </summary>
    public float MaxSize { get; set; }

    /// <summary> 轨道相对父容器 InnerBounds 的起始偏移。 </summary>
    public float Offset { get; set; }
}
