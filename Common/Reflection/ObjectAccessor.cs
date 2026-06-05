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
    private readonly Dictionary<TypedAccessorKey, Delegate> _typedGetters = [];
    private readonly Dictionary<TypedAccessorKey, Delegate> _typedSetters = [];
    private readonly IReadOnlyDictionary<string, PropertyInfo> _propertyInfos;
    private readonly IReadOnlyDictionary<string, MemberInfo> _memberInfos;
    private readonly IReadOnlyDictionary<string, Type> _memberTypes;

    /// <summary>
    /// 使用已扫描好的成员元数据和 object 访问器创建实例。
    /// </summary>
    /// <param name="type">访问器对应的引用类型。</param>
    /// <param name="getters">按成员名索引的 object getter。</param>
    /// <param name="setters">按成员名索引的 object setter。</param>
    /// <param name="propertyInfos">按属性名索引的属性元数据。</param>
    /// <param name="memberInfos">按成员名索引的属性或字段元数据。</param>
    /// <param name="memberTypes">按成员名索引的成员值类型。</param>
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
    /// 获取指定实例成员的强类型 getter 委托。
    /// </summary>
    /// <typeparam name="TTarget">访问委托接收的目标类型。</typeparam>
    /// <typeparam name="TValue">访问委托返回的成员值类型，必须与成员真实类型完全一致。</typeparam>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>接收目标实例并返回成员值的强类型委托。</returns>
    public Func<TTarget, TValue> GetTypedGetter<TTarget, TValue>(string memberName)
    {
        var key = new TypedAccessorKey(memberName, typeof(TTarget), typeof(TValue));

        // 同一个成员可能被 object 入口和具体类型入口同时使用，强类型委托需要按完整签名分别缓存。
        if (_typedGetters.TryGetValue(key, out var getter))
            return (Func<TTarget, TValue>)getter;

        ValidateTypedMember<TValue>(memberName);

        if (!_memberInfos.TryGetValue(memberName, out var memberInfo) || !HasGetter(memberInfo))
            throw new InvalidOperationException($"成员 {_type.FullName}.{memberName} 没有可用 getter。");

        var typedGetter = CreateTypedGetter<TTarget, TValue>(memberInfo);
        _typedGetters[key] = typedGetter;
        return typedGetter;
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
    /// 获取指定实例成员的强类型 setter 委托。
    /// </summary>
    /// <typeparam name="TTarget">访问委托接收的目标类型。</typeparam>
    /// <typeparam name="TValue">访问委托写入的成员值类型，必须与成员真实类型完全一致。</typeparam>
    /// <param name="memberName">实例属性名或字段名。</param>
    /// <returns>接收目标实例和值并写入成员的强类型委托。</returns>
    public Action<TTarget, TValue> GetTypedSetter<TTarget, TValue>(string memberName)
    {
        var key = new TypedAccessorKey(memberName, typeof(TTarget), typeof(TValue));

        // setter 的目标类型和值类型都会影响委托签名，不能只按成员名复用。
        if (_typedSetters.TryGetValue(key, out var setter))
            return (Action<TTarget, TValue>)setter;

        ValidateTypedMember<TValue>(memberName);

        if (!_memberInfos.TryGetValue(memberName, out var memberInfo) || !HasSetter(memberInfo))
            throw new InvalidOperationException($"成员 {_type.FullName}.{memberName} 没有可用 setter。");

        var typedSetter = CreateTypedSetter<TTarget, TValue>(memberInfo);
        _typedSetters[key] = typedSetter;
        return typedSetter;
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

    /// <summary>
    /// 从当前类型开始向基类逐级枚举实例属性。
    /// </summary>
    /// <param name="type">开始枚举的类型。</param>
    /// <param name="bindingFlags">基础反射筛选标志。</param>
    /// <returns>按派生类到基类顺序返回的属性列表。</returns>
    private static IEnumerable<PropertyInfo> GetInstanceProperties(Type type, BindingFlags bindingFlags)
    {
        // 用 DeclaredOnly 手动逐级枚举，避免反射默认继承行为影响非公开成员和同名隐藏成员的优先级。
        for (var currentType = type; currentType is not null; currentType = currentType.BaseType)
        {
            foreach (var property in currentType.GetProperties(bindingFlags | BindingFlags.DeclaredOnly))
                yield return property;
        }
    }

    /// <summary>
    /// 从当前类型开始向基类逐级枚举实例字段。
    /// </summary>
    /// <param name="type">开始枚举的类型。</param>
    /// <param name="bindingFlags">基础反射筛选标志。</param>
    /// <returns>按派生类到基类顺序返回的字段列表。</returns>
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

    /// <summary>
    /// 为属性 getter 创建 object 形态的已编译访问委托。
    /// </summary>
    /// <param name="type">访问器对应的引用类型。</param>
    /// <param name="getMethod">属性 getter 方法。</param>
    /// <returns>接收 object 目标并返回 object 值的 getter。</returns>
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

    /// <summary>
    /// 为属性 setter 创建 object 形态的已编译访问委托。
    /// </summary>
    /// <param name="type">访问器对应的引用类型。</param>
    /// <param name="propertyType">属性值类型。</param>
    /// <param name="setMethod">属性 setter 方法。</param>
    /// <returns>接收 object 目标和 object 值的 setter。</returns>
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

    /// <summary>
    /// 为字段读取创建 object 形态的已编译访问委托。
    /// </summary>
    /// <param name="type">访问器对应的引用类型。</param>
    /// <param name="field">字段元数据。</param>
    /// <returns>接收 object 目标并返回 object 值的 getter。</returns>
    private static Func<object, object> CreateFieldGetter(Type type, FieldInfo field)
    {
        var targetParameter = Expression.Parameter(typeof(object), "target");
        var typedTarget = Expression.Convert(targetParameter, type);
        var fieldAccess = Expression.Field(typedTarget, field);
        // 字段 getter 与属性 getter 一样，返回值统一装箱到 object。
        var boxedValue = Expression.Convert(fieldAccess, typeof(object));

        return Expression.Lambda<Func<object, object>>(boxedValue, targetParameter).Compile();
    }

    /// <summary>
    /// 为字段写入创建 object 形态的已编译访问委托。
    /// </summary>
    /// <param name="type">访问器对应的引用类型。</param>
    /// <param name="field">字段元数据。</param>
    /// <returns>接收 object 目标和 object 值的 setter。</returns>
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

    /// <summary>
    /// 验证强类型访问器的值类型与成员真实类型完全一致。
    /// </summary>
    /// <typeparam name="TValue">调用方期望的成员值类型。</typeparam>
    /// <param name="memberName">实例属性名或字段名。</param>
    private void ValidateTypedMember<TValue>(string memberName)
    {
        var memberType = GetMemberType(memberName);
        if (memberType == typeof(TValue)) return;

        throw new InvalidOperationException(
            $"成员 {_type.FullName}.{memberName} 是 {memberType.FullName}，无法创建值类型为 {typeof(TValue).FullName} 的强类型访问器。");
    }

    /// <summary>
    /// 判断成员是否能读取。
    /// </summary>
    /// <param name="memberInfo">属性或字段元数据。</param>
    /// <returns>成员存在可用 getter 或为字段时返回 <see langword="true"/>。</returns>
    private static bool HasGetter(MemberInfo memberInfo)
    {
        return memberInfo switch
        {
            PropertyInfo property => property.GetGetMethod(true) is not null,
            FieldInfo => true,
            _ => false,
        };
    }

    /// <summary>
    /// 判断成员是否能写入。
    /// </summary>
    /// <param name="memberInfo">属性或字段元数据。</param>
    /// <returns>属性存在 setter，或字段不是 readonly/const 时返回 <see langword="true"/>。</returns>
    private static bool HasSetter(MemberInfo memberInfo)
    {
        return memberInfo switch
        {
            PropertyInfo property => property.GetSetMethod(true) is not null,
            FieldInfo field => !field.IsInitOnly && !field.IsLiteral,
            _ => false,
        };
    }

    /// <summary>
    /// 为属性或字段创建强类型 getter 委托。
    /// </summary>
    /// <typeparam name="TTarget">访问委托接收的目标类型。</typeparam>
    /// <typeparam name="TValue">访问委托返回的成员值类型。</typeparam>
    /// <param name="memberInfo">属性或字段元数据。</param>
    /// <returns>接收 <typeparamref name="TTarget"/> 并返回 <typeparamref name="TValue"/> 的 getter。</returns>
    private Func<TTarget, TValue> CreateTypedGetter<TTarget, TValue>(MemberInfo memberInfo)
    {
        var targetParameter = Expression.Parameter(typeof(TTarget), "target");
        // TTarget 可能是 object，也可能是派生/接口入口；调用实际成员前统一转换为访问器创建时的类型。
        var typedTarget = Expression.Convert(targetParameter, _type);

        Expression value = memberInfo switch
        {
            PropertyInfo property => Expression.Call(typedTarget, property.GetGetMethod(true)!),
            FieldInfo field => Expression.Field(typedTarget, field),
            _ => throw new InvalidOperationException($"成员 {_type.FullName}.{memberInfo.Name} 没有可用 getter。"),
        };

        return Expression.Lambda<Func<TTarget, TValue>>(value, targetParameter).Compile();
    }

    /// <summary>
    /// 为属性或字段创建强类型 setter 委托。
    /// </summary>
    /// <typeparam name="TTarget">访问委托接收的目标类型。</typeparam>
    /// <typeparam name="TValue">访问委托写入的成员值类型。</typeparam>
    /// <param name="memberInfo">属性或字段元数据。</param>
    /// <returns>接收 <typeparamref name="TTarget"/> 和 <typeparamref name="TValue"/> 的 setter。</returns>
    private Action<TTarget, TValue> CreateTypedSetter<TTarget, TValue>(MemberInfo memberInfo)
    {
        var targetParameter = Expression.Parameter(typeof(TTarget), "target");
        var valueParameter = Expression.Parameter(typeof(TValue), "value");
        // valueParameter 已经是成员真实类型，表达式里不再转换成 object，值类型路径不会装箱。
        var typedTarget = Expression.Convert(targetParameter, _type);

        Expression assign = memberInfo switch
        {
            PropertyInfo property => Expression.Call(typedTarget, property.GetSetMethod(true)!, valueParameter),
            FieldInfo field => Expression.Assign(Expression.Field(typedTarget, field), valueParameter),
            _ => throw new InvalidOperationException($"成员 {_type.FullName}.{memberInfo.Name} 没有可用 setter。"),
        };

        return Expression.Lambda<Action<TTarget, TValue>>(assign, targetParameter, valueParameter).Compile();
    }

    /// <summary>
    /// 强类型访问器缓存键。
    /// </summary>
    /// <param name="MemberName">实例属性名或字段名。</param>
    /// <param name="TargetType">委托目标参数类型。</param>
    /// <param name="ValueType">委托值参数或返回值类型。</param>
    private readonly record struct TypedAccessorKey(string MemberName, Type TargetType, Type ValueType);

    #endregion
}
