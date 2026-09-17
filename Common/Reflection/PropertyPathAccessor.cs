namespace SilkyUIFramework.Common.Reflection;

/// <summary>
/// 嵌套属性访问器
/// </summary>
public sealed class PropertyPathAccessor
{
    private sealed class MemberAccess(ObjectAccessor accessor, string memberName)
    {
        public Type ValueType { get; } = accessor.GetMemberType(memberName);

        public Func<object, object> Getter => field ??= accessor.GetGetter(memberName);
        public Action<object, object> Setter => field ??= accessor.GetSetter(memberName);
    }

    public Type RootType { get; }
    private readonly string[] _propertyPath;
    public string[] PropertyPath => [.. _propertyPath];
    private readonly Dictionary<(int SegmentIndex, Type RuntimeType), MemberAccess> _memberCache = [];

    /// <summary>
    /// 根据根类型创建嵌套属性访问器；成员在访问时按实际运行时类型解析。
    /// </summary>
    /// <param name="rootType">允许使用的根对象类型。</param>
    /// <param name="propertyPath">属性段列表，不能为空且每段不能为空白。</param>
    /// <exception cref="ArgumentException"><paramref name="propertyPath"/> 为空或含空白段。</exception>
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
    /// 根据每一层对象的实际运行时类型获取嵌套成员值。
    /// </summary>
    /// <param name="root">根对象。</param>
    /// <returns>成员值；根对象或中间节点为 null 时返回 null。</returns>
    /// <exception cref="ArgumentException"><paramref name="root"/> 的类型与 <see cref="RootType"/> 不兼容。</exception>
    /// <exception cref="InvalidOperationException">当前运行时类型中不存在路径成员，或成员不可读。</exception>
    public object GetValue(object root) => TryGetValue(root, out var value, out _) ? value : null;

    /// <summary>
    /// 尝试获取嵌套成员值及其在当前运行时类型上的声明类型。
    /// </summary>
    /// <param name="root">根对象。</param>
    /// <param name="value">读取成功时为末端成员值；该值本身可以为 null。</param>
    /// <param name="valueType">读取成功时为末端成员的声明类型。</param>
    /// <returns>读取成功时返回 true，包括末端值为 null；仅根对象或中间节点为 null 时返回 false。</returns>
    /// <exception cref="ArgumentException"><paramref name="root"/> 的类型与 <see cref="RootType"/> 不兼容。</exception>
    /// <exception cref="InvalidOperationException">当前运行时类型中不存在路径成员，或成员不可读。</exception>
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
    /// 尝试设置嵌套成员值，并返回其在当前运行时类型上的声明类型。
    /// </summary>
    /// <param name="root">根对象。</param>
    /// <param name="value">要写入末端成员的值。</param>
    /// <param name="valueType">写入成功时为末端成员的声明类型。</param>
    /// <returns>调用 setter 成功时返回 true；仅根对象或中间节点为 null 时返回 false。</returns>
    /// <exception cref="ArgumentException"><paramref name="root"/> 的类型与 <see cref="RootType"/> 不兼容。</exception>
    /// <exception cref="InvalidOperationException">路径成员不存在、中间成员不可读或末端成员不可写。</exception>
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
    /// 返回的对象可用于固定绑定动画，后续替换路径中的对象不会改变这次绑定。
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