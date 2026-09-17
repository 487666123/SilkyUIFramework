using System.Collections;

namespace SilkyUIFramework.StyleSystem;

/// <summary>
/// 属性路径到目标值的映射，可以在多个元素间共享。
/// 修改后在下一次 ApplyStyle 时生效；定义本身不持有元素或动画。
/// </summary>
public class StyleDefinition : IEnumerable<KeyValuePair<string, object>>
{
    private readonly Dictionary<string, object> _values = [with(StringComparer.Ordinal)];

    public object this[string propertyPath]
    {
        get => _values[propertyPath];
        set => _values[propertyPath] = value;
    }

    /// <summary>设置属性或字段，支持嵌套路径，例如 RectangleRender.BackgroundColor。</summary>
    public StyleDefinition Set(string propertyName, object value)
    {
        this[propertyName] = value;
        return this;
    }

    public StyleDefinition SetRange(params (string propertyName, object value)[] properties)
    {
        foreach (var (propertyName, value) in properties) this[propertyName] = value;
        return this;
    }

    public Dictionary<string, object>.Enumerator GetEnumerator() => _values.GetEnumerator();
    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
