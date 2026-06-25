namespace SilkyUIFramework.Layout;

/// <summary>
/// 定义 Grid 子项在单个轴上的起始轨道与跨度。
/// 起始轨道为 null 表示参与自动放置。
/// </summary>
public readonly struct GridSpan(int? start, int size) : IEquatable<GridSpan>
{
    /// <summary> 起始轨道索引；为 null 时由 Grid 自动放置算法决定。 </summary>
    public int? Start { get; } = start;

    /// <summary> 跨越的轨道数量，最小为 1，且不会让明确放置越过最大轨道数。 </summary>
    public int Size { get; } = size;

    /// <summary> 自动放置且跨度为 1 的默认值。 </summary>
    public static GridSpan Auto { get; } = new(null, 1);

    /// <summary> 创建明确起点的 GridSpan。 </summary>
    public static GridSpan At(int start, int size = 1) => new(start, size);

    public static bool operator ==(GridSpan left, GridSpan right) => left.Equals(right);

    public static bool operator !=(GridSpan left, GridSpan right) => !left.Equals(right);

    public bool Equals(GridSpan other) => Start == other.Start && Size == other.Size;

    public override bool Equals(object obj) => obj is GridSpan other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, Size);
}
