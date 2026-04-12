namespace SilkyUIFramework.Common.Reflection;

public static class ObjectAccessorCache
{
    private static readonly Dictionary<Type, ObjectAccessor> _accessorCache = [];

    public static ObjectAccessor GetAccessor(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.IsValueType)
            throw new NotSupportedException($"不支持为值类型 {type.FullName} 创建对象访问器。");

        if (_accessorCache.TryGetValue(type, out var accessor)) return accessor;

        return _accessorCache[type] = ObjectAccessor.Create(type);
    }
}
