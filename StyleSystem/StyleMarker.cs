namespace SilkyUIFramework.StyleSystem;

/// <summary>
/// 一个可扩展的样式匹配标记。标记之间可以同时存在，名称按区分大小写的序列比较。
/// </summary>
public readonly record struct StyleMarker
{
    public string Name { get; }

    public StyleMarker(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Name);

    public override string ToString() => Name ?? string.Empty;
}

/// <summary>框架内置的常用样式标记。</summary>
public static class StyleMarkers
{
    public static readonly StyleMarker Hover = new("hover");
    public static readonly StyleMarker Active = new("active");
    public static readonly StyleMarker Focus = new("focus");
    public static readonly StyleMarker Disabled = new("disabled");
    public static readonly StyleMarker Selected = new("selected");

    public static readonly StyleMarker Checked = new("checked");
}
