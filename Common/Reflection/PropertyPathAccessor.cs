namespace SilkyUIFramework.Common.Reflection;

/// <summary>
/// 按各层对象的运行时类型访问嵌套属性或字段。
/// </summary>
/// <remarks>
/// 根对象或中间节点为 null 时停止访问；根类型不兼容、成员缺失或所需读写操作不可用时抛出异常。
/// </remarks>
public sealed class PropertyPathAccessor
{
    // 缓存单个成员的声明类型，并按需获取读写委托。
    private sealed class MemberAccess(ObjectAccessor accessor, string memberName)
    {
        /// <summary>成员的声明类型，不随当前值变化。</summary>
        public Type ValueType { get; } = accessor.GetMemberType(memberName);

        /// <summary>首次读取时获取并缓存 getter。</summary>
        public Func<object, object> Getter => field ??= accessor.GetGetter(memberName);
        /// <summary>首次写入时获取并缓存 setter。</summary>
        public Action<object, object> Setter => field ??= accessor.GetSetter(memberName);
    }

    /// <summary>根对象必须兼容的类型。</summary>
    public Type RootType { get; }
    // 保存路径副本，避免外部修改影响成员解析和缓存。
    private readonly string[] _propertyPath;
    /// <summary>返回成员路径的副本。</summary>
    public string[] PropertyPath => [.. _propertyPath];
    // 同一路径段可能遇到不同的派生类型，需要分别缓存。
    private readonly Dictionary<(int SegmentIndex, Type RuntimeType), MemberAccess> _memberCache = [];

    /// <summary>
    /// 保存根类型和成员路径；路径不能为空，各段不能为空白，成员在访问时解析。
    /// </summary>
    public PropertyPathAccessor(Type rootType, string[] propertyPath)
    {
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(propertyPath);

        if (propertyPath.Length == 0)
            throw new ArgumentException("属性段列表不能为空。", nameof(propertyPath));

        if (propertyPath.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("属性段不能为空。", nameof(propertyPath));

        RootType = rootType;
        _propertyPath = [.. propertyPath];
    }

    /// <summary>
    /// 获取末端成员值；根对象或中间节点为 null 时返回 null。
    /// </summary>
    public object GetValue(object root) => TryGetValue(root, out var value, out _) ? value : null;

    /// <summary>
    /// 读取末端成员值及其声明类型；末端值为 null 也视为成功。
    /// </summary>
    /// <returns>根对象或中间节点为 null 时返回 false，否则返回 true。</returns>
    public bool TryGetValue(object root, out object value, out Type valueType)
    {
        value = null;
        valueType = null;

        if (!TryResolveFinalMember(root, out var owner, out var member))
            return false;

        valueType = member.ValueType;
        value = member.Getter(owner);
        return true;
    }

    /// <summary>
    /// 写入末端成员值，并返回其声明类型。
    /// </summary>
    /// <returns>根对象或中间节点为 null 时返回 false，写入成功时返回 true。</returns>
    public bool TrySetValue(object root, object value, out Type valueType)
    {
        valueType = null;

        if (!TryResolveFinalMember(root, out var owner, out var member))
            return false;

        valueType = member.ValueType;
        member.Setter(owner, value);
        return true;
    }

    /// <summary>
    /// 解析末端成员的所属对象、名称和声明类型；根或中间节点为 null 时返回 false。
    /// 返回当前所属对象，后续替换路径节点不会更新该对象引用。
    /// </summary>
    public bool TryResolveMember(object root, out object owner, out string memberName, out Type valueType)
    {
        memberName = null;
        valueType = null;
        if (!TryResolveFinalMember(root, out owner, out var member)) return false;

        memberName = _propertyPath[^1];
        valueType = member.ValueType;
        return true;
    }

    /// <summary>遍历中间节点并解析末端成员，不读取末端值；遇到 null 节点时返回 false。</summary>
    private bool TryResolveFinalMember(object root, out object owner, out MemberAccess member)
    {
        owner = null;
        member = null;

        if (root == null) return false;

        if (!RootType.IsInstanceOfType(root))
            throw new ArgumentException($"根对象类型 {root.GetType().FullName} 与访问器根类型 {RootType.FullName} 不兼容。", nameof(root));

        var current = root;

        for (var i = 0; i < _propertyPath.Length - 1; i++)
        {
            var parentMember = GetOrCreateMemberAccess(i, current.GetType());
            current = parentMember.Getter(current);

            if (current == null) return false;
        }

        owner = current;
        member = GetOrCreateMemberAccess(_propertyPath.Length - 1, current.GetType());
        return true;
    }

    /// <summary>按路径段索引和所属对象的运行时类型复用成员访问器。</summary>
    private MemberAccess GetOrCreateMemberAccess(int segmentIndex, Type runtimeType)
    {
        var key = (SegmentIndex: segmentIndex, RuntimeType: runtimeType);
        if (_memberCache.TryGetValue(key, out var member)) return member;

        var accessor = ObjectAccessorCache.GetAccessorByType(runtimeType);
        member = new MemberAccess(accessor, _propertyPath[segmentIndex]);
        _memberCache[key] = member;
        return member;
    }
}