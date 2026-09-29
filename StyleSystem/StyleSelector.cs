namespace SilkyUIFramework.StyleSystem;

/// <summary>
/// 样式规则的匹配条件。所有 RequiredMarkers 都存在时规则才匹配。
/// </summary>
public sealed class StyleSelector : IEquatable<StyleSelector>
{
    private readonly StyleMarker[] _requiredMarkers;

    public static StyleSelector Empty { get; } = new([]);

    public IReadOnlyList<StyleMarker> RequiredMarkers => _requiredMarkers;

    public int Specificity => _requiredMarkers.Length;

    public StyleSelector(IEnumerable<StyleMarker> requiredMarkers)
    {
        ArgumentNullException.ThrowIfNull(requiredMarkers);

        _requiredMarkers = [.. requiredMarkers
            .Select(marker =>
            {
                if (!marker.IsValid)
                    throw new ArgumentException("样式标记不能为空。", nameof(requiredMarkers));
                return marker;
            })
            .Distinct()
            .OrderBy(marker => marker.Name, StringComparer.Ordinal)];
    }

    public static StyleSelector AllOf(params StyleMarker[] markers) => new(markers);

    public bool Matches(ISet<StyleMarker> activeMarkers)
    {
        ArgumentNullException.ThrowIfNull(activeMarkers);
        return _requiredMarkers.All(activeMarkers.Contains);
    }

    public bool Equals(StyleSelector other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null || _requiredMarkers.Length != other._requiredMarkers.Length) return false;
        return _requiredMarkers.SequenceEqual(other._requiredMarkers);
    }

    public override bool Equals(object obj) => Equals(obj as StyleSelector);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var marker in _requiredMarkers) hash.Add(marker);
        return hash.ToHashCode();
    }
}
