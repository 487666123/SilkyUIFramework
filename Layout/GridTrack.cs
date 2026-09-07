namespace SilkyUIFramework.Layout;

/// <summary>
/// 定义单个 Grid 轨道。
/// </summary>
public readonly struct GridTrack(TemplateType templateType, float value = 0f) : IEquatable<GridTrack>
{
    /// <summary> 轨道尺寸类型。 </summary>
    public TemplateType TemplateType { get; } = templateType;

    /// <summary> 轨道类型对应的数值，例如像素值、百分比或 fr 权重。 </summary>
    public float Value { get; } = value;

    /// <summary> 创建 Auto 轨道。 </summary>
    public static GridTrack Auto => new(TemplateType.Auto);

    /// <summary> 创建 Fraction 轨道。 </summary>
    public static GridTrack Fr(float value = 1f) => new(TemplateType.Fraction, value);

    /// <summary> 创建固定像素轨道。 </summary>
    public static GridTrack Pixels(float value) => new(TemplateType.Pixels, value);

    /// <summary> 创建百分比轨道，value 按父容器对应轴尺寸的倍率使用，不在这里限制到 0 到 1。 </summary>
    public static GridTrack Percent(float value) => new(TemplateType.Percent, value);

    /// <summary>
    /// 创建一组重复轨道。
    /// </summary>
    public static GridTrack[] Repeat(int quantity, TemplateType templateType, float value = 0f)
    {
        if (quantity <= 0) return [];

        var tracks = new GridTrack[quantity];
        for (var i = 0; i < tracks.Length; i++)
        {
            tracks[i] = new GridTrack(templateType, value);
        }

        return tracks;
    }

    public static bool operator ==(GridTrack left, GridTrack right) => left.Equals(right);

    public static bool operator !=(GridTrack left, GridTrack right) => !left.Equals(right);

    public bool Equals(GridTrack other) => TemplateType == other.TemplateType && Value.Equals(other.Value);

    public override bool Equals(object obj) => obj is GridTrack other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(TemplateType, Value);
}
