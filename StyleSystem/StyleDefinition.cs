using System.Collections;

namespace SilkyUIFramework.StyleSystem;

/// <summary>
/// 属性路径到强类型目标值的映射，可以在多个元素间共享。
/// 修改后在下一次 ApplyStyle 时生效；定义本身不持有元素或动画。
/// </summary>
public class StyleDefinition : IEnumerable<KeyValuePair<string, StyleValue>>
{
    private readonly Dictionary<string, StyleValue> _values = [with(StringComparer.Ordinal)];

    /// <summary>获取或替换样式值；可使用 StyleValue.Create(value) 保留目标值的泛型类型。</summary>
    public StyleValue this[string propertyPath]
    {
        get => _values[propertyPath];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _values[propertyPath] = value;
        }
    }

    /// <summary>设置属性或字段，支持嵌套路径，例如 RectangleRender.BackgroundColor。</summary>
    /// <typeparam name="T">必须与成员声明类型完全一致；null、基类和接口值可显式指定 T。</typeparam>
    public StyleDefinition Set<T>(string propertyName, T value)
    {
        this[propertyName] = StyleValue.Create(value);
        return this;
    }

    /// <summary>批量设置已包装的样式值，每一项通过 StyleValue.Create(value) 独立保留类型。</summary>
    public StyleDefinition SetRange(params (string propertyName, StyleValue value)[] properties)
    {
        foreach (var (propertyName, value) in properties) this[propertyName] = value;
        return this;
    }

    public Dictionary<string, StyleValue>.Enumerator GetEnumerator() => _values.GetEnumerator();
    IEnumerator<KeyValuePair<string, StyleValue>> IEnumerable<KeyValuePair<string, StyleValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
