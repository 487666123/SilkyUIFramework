using SilkyUIFramework.StyleSystem;

namespace SilkyUIFramework.Elements;

public partial class UIView
{
    private readonly HashSet<StyleMarker> _styleMarkers = [];

    public IReadOnlyCollection<StyleMarker> ActiveStyleMarkers => _styleMarkers;

    /// <summary>此元素专属的样式表，首次访问时创建并固定绑定到当前元素。</summary>
    public UIStyleSheet StyleSheet => field ??= new UIStyleSheet(this);

    public bool HasStyleMarker(StyleMarker marker) => _styleMarkers.Contains(marker);

    public void AddStyleMarker(StyleMarker marker)
    {
        if (!marker.IsValid) throw new ArgumentException("样式标记不能为空。", nameof(marker));
        if (!_styleMarkers.Add(marker)) return;
        ApplyCurrentStyle();
    }

    public void RemoveStyleMarker(StyleMarker marker)
    {
        if (!marker.IsValid) throw new ArgumentException("样式标记不能为空。", nameof(marker));
        if (!_styleMarkers.Remove(marker)) return;
        ApplyCurrentStyle();
    }

    public void SetStyleMarker(StyleMarker marker, bool enabled)
    {
        if (enabled) AddStyleMarker(marker);
        else RemoveStyleMarker(marker);
    }

    private void ApplyCurrentStyle() => StyleSheet?.ApplyStyle(_styleMarkers);
}
