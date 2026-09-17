namespace SilkyUIFramework.Common.Reflection;

/// <summary>
/// 按引用类型缓存对象成员访问器，供同类型的对象复用。
/// </summary>
public static class ObjectAccessorCache
{
    private static readonly Dictionary<Type, ObjectAccessor> _accessorCache = [];

    /// <summary>
    /// 获取指定引用类型的成员访问器；缓存不存在时创建并保存。
    /// </summary>
    /// <param name="type">需要访问成员的引用类型。</param>
    /// <returns>该类型对应的缓存访问器。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> 为 null。</exception>
    /// <exception cref="NotSupportedException"><paramref name="type"/> 是值类型。</exception>
    public static ObjectAccessor GetAccessorByType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.IsValueType)
            throw new NotSupportedException($"不支持为值类型 {type.FullName} 创建对象访问器。");

        if (_accessorCache.TryGetValue(type, out var accessor)) return accessor;

        return _accessorCache[type] = ObjectAccessor.Create(type);
    }

    /// <summary>
    /// 根据对象的实际运行时类型获取成员访问器；缓存不存在时创建并保存。
    /// </summary>
    /// <param name="obj">需要访问成员的对象实例，不能为 null。</param>
    /// <returns>对象实际运行时类型对应的缓存访问器。</returns>
    /// <exception cref="NotSupportedException"><paramref name="obj"/> 是装箱后的值类型实例。</exception>
    public static ObjectAccessor GetAccessor(object obj) => GetAccessorByType(obj.GetType());

    /// <summary>
    /// 根据泛型参数指定的类型获取成员访问器；缓存不存在时创建并保存。
    /// </summary>
    /// <typeparam name="T">需要访问成员的引用类型。</typeparam>
    /// <returns><typeparamref name="T"/> 对应的缓存访问器。</returns>
    /// <exception cref="NotSupportedException"><typeparamref name="T"/> 是值类型。</exception>
    public static ObjectAccessor GetAccessor<T>() => GetAccessorByType(typeof(T));
}
