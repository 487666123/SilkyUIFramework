namespace SilkyUIFramework.Layout;

/// <summary>
/// 定义 Grid 轨道的一侧尺寸规则。
/// </summary>
public readonly struct GridTrackSize(TemplateType templateType, float value = 0f) : IEquatable<GridTrackSize>
{
    public TemplateType TemplateType { get; } = templateType;

    public float Value { get; } = value;

    public static GridTrackSize Auto => new(TemplateType.Auto);

    public static GridTrackSize Fr(float value = 1f) => new(TemplateType.Fraction, value);

    public static GridTrackSize Pixels(float value) => new(TemplateType.Pixels, value);

    public static GridTrackSize Percent(float value) => new(TemplateType.Percent, value);

    public static bool operator ==(GridTrackSize left, GridTrackSize right) => left.Equals(right);

    public static bool operator !=(GridTrackSize left, GridTrackSize right) => !left.Equals(right);

    public bool Equals(GridTrackSize other) =>
        TemplateType == other.TemplateType && Value.Equals(other.Value);

    public override bool Equals(object obj) => obj is GridTrackSize other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(TemplateType, Value);
}

/// <summary>
/// 定义单个 Grid 轨道的最小和最大尺寸规则。
/// </summary>
public readonly struct GridTrack : IEquatable<GridTrack>
{
    public GridTrackSize Min { get; }

    public GridTrackSize Max { get; }

    private GridTrack(GridTrackSize min, GridTrackSize max)
    {
        Min = min;
        Max = max;
    }

    public static GridTrack Auto =>
        new(GridTrackSize.Auto, GridTrackSize.Auto);

    public static GridTrack Fr(float value = 1f) =>
        new(GridTrackSize.Pixels(0f), GridTrackSize.Fr(value));

    public static GridTrack Pixels(float value) =>
        new(GridTrackSize.Pixels(value), GridTrackSize.Pixels(value));

    public static GridTrack Percent(float value) =>
        new(GridTrackSize.Percent(value), GridTrackSize.Percent(value));

    public static GridTrack MinMax(GridTrackSize min, GridTrackSize max)
    {
        ValidateMin(min);
        ValidateSize(max, nameof(max));
        return new GridTrack(min, max);
    }

    public static GridTrack[] Repeat(int quantity, TemplateType templateType, float value = 0f)
    {
        if (quantity <= 0) return [];

        var track = CreateSingle(templateType, value);
        var tracks = new GridTrack[quantity];
        Array.Fill(tracks, track);
        return tracks;
    }

    public static bool operator ==(GridTrack left, GridTrack right) => left.Equals(right);

    public static bool operator !=(GridTrack left, GridTrack right) => !left.Equals(right);

    public bool Equals(GridTrack other) => Min == other.Min && Max == other.Max;

    public override bool Equals(object obj) => obj is GridTrack other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Min, Max);

    private static GridTrack CreateSingle(TemplateType templateType, float value) =>
        templateType switch
        {
            TemplateType.Auto => Auto,
            TemplateType.Fraction => Fr(value),
            TemplateType.Pixels => Pixels(value),
            TemplateType.Percent => Percent(value),
            _ => Auto
        };

    private static void ValidateMin(GridTrackSize min)
    {
        if (min.TemplateType is TemplateType.Fraction)
            throw new ArgumentException("A Fraction track cannot be used as MinMax min size.", nameof(min));

        ValidateSize(min, nameof(min));
    }

    private static void ValidateSize(GridTrackSize size, string parameterName)
    {
        if (float.IsNaN(size.Value) || float.IsInfinity(size.Value))
            throw new ArgumentException("Grid track size must be finite.", parameterName);
    }
}
