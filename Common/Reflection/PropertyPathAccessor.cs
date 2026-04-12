namespace SilkyUIFramework.Common.Reflection;

/// <summary>
/// 嵌套属性访问器
/// </summary>
public sealed class PropertyPathAccessor
{
    private readonly Func<object, object>[] _gettersChain;

    /// <summary> 根类型 </summary>
    public Type RootType { get; }

    /// <summary> 属性路径 </summary>
    public string[] PropertyPath { get; }

    /// <summary> 属性段数量 </summary>
    public int SegmentCount => _gettersChain.Length;

    private PropertyPathAccessor(Type rootType, string[] propertyPath, IEnumerable<Func<object, object>> gettersChain)
    {
        RootType = rootType;
        PropertyPath = propertyPath;
        _gettersChain = [.. gettersChain];
    }

    /// <summary>
    /// 获取嵌套属性值
    /// </summary>
    /// <param name="root">根对象</param>
    /// <returns>属性值，中间节点为null时返回null</returns>
    public object GetValue(object root)
    {
        var current = root;
        foreach (var getter in _gettersChain)
        {
            if (current == null) return null;
            current = getter(current);
        }

        return current;
    }

    /// <summary>
    /// 创建嵌套属性访问器
    /// </summary>
    /// <param name="rootType">根类型</param>
    /// <param name="propertyPath">属性段列表</param>
    /// <returns>嵌套属性访问器实例</returns>
    public static PropertyPathAccessor Create(Type rootType, string[] propertyPath)
    {
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(propertyPath);

        if (propertyPath.Length == 0)
            throw new ArgumentException("属性段列表不能为空", nameof(propertyPath));

        var propertyPathString = string.Join(".", propertyPath);

        var getters = new List<Func<object, object>>(propertyPath.Length);
        var currentType = rootType;

        foreach (var segment in propertyPath)
        {
            var accessor = ObjectAccessorCache.GetAccessor(currentType);
            var getter = accessor.GetGetter(segment);
            var propertyInfo = accessor.GetPropertyInfo(segment);

            getters.Add(getter);
            currentType = propertyInfo.PropertyType;
        }

        return new PropertyPathAccessor(rootType, propertyPath, getters);
    }
}