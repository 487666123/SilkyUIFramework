using System.Linq.Expressions;

namespace SilkyUIFramework.Common.Reflection;

/// <summary>
/// 为指定引用类型缓存实例成员的快速访问委托。
/// </summary>
/// <remarks>
/// 支持公开与非公开实例属性、实例字段；索引器会被忽略，值类型不支持创建访问器。
/// </remarks>
public sealed class ObjectAccessor
{
    private readonly Type _type;
    private readonly IReadOnlyDictionary<string, Func<object, object>> _getters;
    private readonly IReadOnlyDictionary<string, Action<object, object>> _setters;
    private readonly IReadOnlyDictionary<string, PropertyInfo> _propertyInfos;
    private readonly IReadOnlyDictionary<string, MemberInfo> _memberInfos;
    private readonly IReadOnlyDictionary<string, Type> _memberTypes;

    private ObjectAccessor(
        Type type,
        IReadOnlyDictionary<string, Func<object, object>> getters,
        IReadOnlyDictionary<string, Action<object, object>> setters,
        IReadOnlyDictionary<string, PropertyInfo> propertyInfos,
        IReadOnlyDictionary<string, MemberInfo> memberInfos,
        IReadOnlyDictionary<string, Type> memberTypes)
    {
        _type = type;
        _getters = getters;
        _setters = setters;
        _propertyInfos = propertyInfos;
        _memberInfos = memberInfos;
        _memberTypes = memberTypes;
    }

    /// <summary>
    /// 获取或设置指定对象上的实例成员值。
    /// </summary>
    /// <param name="obj">要访问的对象实例。</param>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>成员当前值。</returns>
    public object this[object obj, string memberName]
    {
        get => GetValue(obj, memberName);
        set => SetValue(obj, memberName, value);
    }

    /// <summary>
    /// 获取指定对象上的实例成员值。
    /// </summary>
    /// <param name="obj">要访问的对象实例。</param>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>成员当前值。</returns>
    public object GetValue(object obj, string memberName)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return GetGetter(memberName).Invoke(obj);
    }

    /// <summary>
    /// 设置指定对象上的实例成员值。
    /// </summary>
    /// <param name="obj">要访问的对象实例。</param>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <param name="value">要写入的值。</param>
    public void SetValue(object obj, string memberName, object value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        GetSetter(memberName).Invoke(obj, value);
    }

    /// <summary>
    /// 获取指定实例成员的已编译 getter 委托。
    /// </summary>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>接收对象实例并返回成员值的委托。</returns>
    public Func<object, object> GetGetter(string memberName)
    {
        if (_getters.TryGetValue(memberName, out var getter)) return getter;
        throw new InvalidOperationException($"成员 {_type.FullName}.{memberName} 没有可用 getter。");
    }

    /// <summary>
    /// 获取指定实例成员的已编译 setter 委托。
    /// </summary>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>接收对象实例和值并写入成员的委托。</returns>
    public Action<object, object> GetSetter(string memberName)
    {
        if (_setters.TryGetValue(memberName, out var setter)) return setter;
        throw new InvalidOperationException($"成员 {_type.FullName}.{memberName} 没有可用 setter。");
    }

    /// <summary>
    /// 获取指定实例属性的反射信息。
    /// </summary>
    /// <param name="propertyName">实例属性名。</param>
    /// <returns>属性的 <see cref="PropertyInfo"/>。</returns>
    public PropertyInfo GetPropertyInfo(string propertyName)
    {
        if (_propertyInfos.TryGetValue(propertyName, out var propertyInfo)) return propertyInfo;

        throw new InvalidOperationException($"没有可用的 {propertyName} 属性");
    }

    /// <summary>
    /// 获取指定实例成员的反射信息。
    /// </summary>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>成员的 <see cref="MemberInfo"/>。</returns>
    public MemberInfo GetMemberInfo(string memberName)
    {
        if (_memberInfos.TryGetValue(memberName, out var memberInfo)) return memberInfo;

        throw new InvalidOperationException($"没有可用的 {_type.FullName}.{memberName} 成员");
    }

    /// <summary>
    /// 获取指定实例成员的值类型。
    /// </summary>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>属性类型或字段类型。</returns>
    public Type GetMemberType(string memberName)
    {
        if (_memberTypes.TryGetValue(memberName, out var memberType)) return memberType;

        throw new InvalidOperationException($"没有可用的 {_type.FullName}.{memberName} 成员");
    }

    /// <summary>
    /// 为指定引用类型创建对象成员访问器。
    /// </summary>
    /// <param name="type">要分析的引用类型。</param>
    /// <returns>缓存了该类型成员访问委托的 <see cref="ObjectAccessor"/>。</returns>
    public static ObjectAccessor Create(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsValueType) throw new NotSupportedException($"不支持为值类型 {type.FullName} 创建对象访问器。");

        var getters = new Dictionary<string, Func<object, object>>();
        var setters = new Dictionary<string, Action<object, object>>();
        var propertyInfos = new Dictionary<string, PropertyInfo>();
        var memberInfos = new Dictionary<string, MemberInfo>();
        var memberTypes = new Dictionary<string, Type>();

        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var property in GetInstanceProperties(type, bindingFlags))
        {
            // 索引器需要额外参数，无法适配当前 Func<object, object> / Action<object, object> 访问器形状。
            if (property.GetIndexParameters().Length != 0) continue;

            // 成员按“派生类 -> 基类”的顺序枚举；同名成员保留最先遇到的派生类版本。
            if (memberInfos.ContainsKey(property.Name)) continue;

            propertyInfos[property.Name] = property;
            memberInfos[property.Name] = property;
            memberTypes[property.Name] = property.PropertyType;

            if (property.GetGetMethod(true) is { } getMethod)
                getters[property.Name] = CreatePropertyGetter(type, getMethod);

            if (property.GetSetMethod(true) is { } setMethod)
                setters[property.Name] = CreatePropertySetter(type, property.PropertyType, setMethod);
        }

        foreach (var field in GetInstanceFields(type, bindingFlags))
        {
            if (memberInfos.ContainsKey(field.Name)) continue;

            memberInfos[field.Name] = field;
            memberTypes[field.Name] = field.FieldType;

            getters[field.Name] = CreateFieldGetter(type, field);

            // readonly/const 字段不能通过普通赋值表达式安全写入，只暴露 getter。
            if (!field.IsInitOnly && !field.IsLiteral)
                setters[field.Name] = CreateFieldSetter(type, field);
        }

        return new ObjectAccessor(type, getters, setters, propertyInfos, memberInfos, memberTypes);
    }

    private static IEnumerable<PropertyInfo> GetInstanceProperties(Type type, BindingFlags bindingFlags)
    {
        // 用 DeclaredOnly 手动逐级枚举，避免反射默认继承行为影响非公开成员和同名隐藏成员的优先级。
        for (var currentType = type; currentType is not null; currentType = currentType.BaseType)
        {
            foreach (var property in currentType.GetProperties(bindingFlags | BindingFlags.DeclaredOnly))
                yield return property;
        }
    }

    private static IEnumerable<FieldInfo> GetInstanceFields(Type type, BindingFlags bindingFlags)
    {
        // 字段也按同样规则处理，保证派生类型隐藏基类字段时优先使用派生字段。
        for (var currentType = type; currentType is not null; currentType = currentType.BaseType)
        {
            foreach (var field in currentType.GetFields(bindingFlags | BindingFlags.DeclaredOnly))
                yield return field;
        }
    }

    #region Create Getter and Setter

    private static Func<object, object> CreatePropertyGetter(Type type, MethodInfo getMethod)
    {
        var targetParameter = Expression.Parameter(typeof(object), "target");
        // 公开 API 使用 object 参数；编译委托前先还原成声明成员的具体类型。
        var typedTarget = Expression.Convert(targetParameter, type);
        var getterCall = Expression.Call(typedTarget, getMethod);
        // getter 可能返回值类型，统一装箱成 object 作为访问器返回值。
        var boxedValue = Expression.Convert(getterCall, typeof(object));

        return Expression.Lambda<Func<object, object>>(boxedValue, targetParameter).Compile();
    }

    private static Action<object, object> CreatePropertySetter(Type type, Type propertyType, MethodInfo setMethod)
    {
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var valueParameter = Expression.Parameter(typeof(object), "value");

        var typedTarget = Expression.Convert(targetParameter, type);
        // setter 的 value 入口是 object，调用属性 setter 前需要转换回属性真实类型。
        var typedValue = Expression.Convert(valueParameter, propertyType);

        var assign = Expression.Call(typedTarget, setMethod, typedValue);

        return Expression.Lambda<Action<object, object>>(assign, targetParameter, valueParameter).Compile();
    }

    private static Func<object, object> CreateFieldGetter(Type type, FieldInfo field)
    {
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var typedTarget = Expression.Convert(targetParameter, type);
        var fieldAccess = Expression.Field(typedTarget, field);
        // 字段 getter 与属性 getter 一样，返回值统一装箱到 object。
        var boxedValue = Expression.Convert(fieldAccess, typeof(object));

        return Expression.Lambda<Func<object, object>>(boxedValue, targetParameter).Compile();
    }

    private static Action<object, object> CreateFieldSetter(Type type, FieldInfo field)
    {
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var valueParameter = Expression.Parameter(typeof(object), "value");

        var typedTarget = Expression.Convert(targetParameter, type);
        // 字段赋值直接生成 target.Field = (TField)value，避免运行时反射调用开销。
        var typedValue = Expression.Convert(valueParameter, field.FieldType);
        var fieldAccess = Expression.Field(typedTarget, field);
        var assign = Expression.Assign(fieldAccess, typedValue);

        return Expression.Lambda<Action<object, object>>(assign, targetParameter, valueParameter).Compile();
    }

    #endregion
}
