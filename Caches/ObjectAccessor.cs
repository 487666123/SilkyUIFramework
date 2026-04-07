using System.Linq.Expressions;

namespace SilkyUIFramework.Caches;

public sealed class ObjectAccessor
{
    private readonly Type _type;
    private readonly IReadOnlyDictionary<string, Func<object, object>> _getters;
    private readonly IReadOnlyDictionary<string, Action<object, object>> _setters;

    private ObjectAccessor(
        Type type,
        IReadOnlyDictionary<string, Func<object, object>> getters,
        IReadOnlyDictionary<string, Action<object, object>> setters)
    {
        _type = type;
        _getters = getters;
        _setters = setters;
    }

    public static ObjectAccessor Create(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsValueType) throw new NotSupportedException($"不支持为值类型 {type.FullName} 创建对象访问器。");

        Dictionary<string, Func<object, object>> getters = [];
        Dictionary<string, Action<object, object>> setters = [];

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0) continue;

            if (property.GetMethod is { } getMethod)
                getters[property.Name] = CreateGetter(type, getMethod);

            if (property.SetMethod is { } setMethod)
                setters[property.Name] = CreateSetter(type, property.PropertyType, setMethod);
        }

        return new ObjectAccessor(type, getters, setters);
    }

    public object this[object obj, string propName]
    {
        get => GetValue(obj, propName);
        set => SetValue(obj, propName, value);
    }

    public object GetValue(object obj, string propName)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return GetGetter(propName).Invoke(obj);
    }

    public void SetValue(object obj, string propName, object value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        GetSetter(propName).Invoke(obj, value);
    }

    public Func<object, object> GetGetter(string propName)
    {
        if (TryGetGetter(propName, out var getter)) return getter;

        var property = GetProperty(propName);
        if (property.GetIndexParameters().Length != 0)
            throw new NotSupportedException($"不支持访问索引器属性 {_type.FullName}.{propName}。");

        throw new InvalidOperationException($"属性 {_type.FullName}.{propName} 没有公开 getter。");
    }

    public Action<object, object> GetSetter(string propName)
    {
        if (TryGetSetter(propName, out var setter)) return setter;

        var property = GetProperty(propName);
        if (property.GetIndexParameters().Length != 0)
            throw new NotSupportedException($"不支持访问索引器属性 {_type.FullName}.{propName}。");

        throw new InvalidOperationException($"属性 {_type.FullName}.{propName} 没有公开 setter。");
    }

    public bool TryGetGetter(string propName, out Func<object, object> getter)
    {
        ArgumentException.ThrowIfNullOrEmpty(propName);
        return _getters.TryGetValue(propName, out getter);
    }

    public bool TryGetSetter(string propName, out Action<object, object> setter)
    {
        ArgumentException.ThrowIfNullOrEmpty(propName);
        return _setters.TryGetValue(propName, out setter);
    }

    private static Func<object, object> CreateGetter(Type type, MethodInfo getMethod)
    {
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var typedTarget = Expression.Convert(targetParameter, type);
        var getterCall = Expression.Call(typedTarget, getMethod);
        var boxedValue = Expression.Convert(getterCall, typeof(object));

        return Expression.Lambda<Func<object, object>>(boxedValue, targetParameter).Compile();
    }

    private static Action<object, object> CreateSetter(Type type, Type propertyType, MethodInfo setMethod)
    {
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var valueParameter = Expression.Parameter(typeof(object), "value");

        var typedTarget = Expression.Convert(targetParameter, type);
        var typedValue = Expression.Convert(valueParameter, propertyType);

        var assign = Expression.Call(typedTarget, setMethod, typedValue);

        return Expression.Lambda<Action<object, object>>(assign, targetParameter, valueParameter).Compile();
    }

    private PropertyInfo GetProperty(string propName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propName);
        return _type.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"类型 {_type.FullName} 上不存在属性 {propName}。");
    }
}
