using SilkyUIFramework.Common.Reflection;
using SilkyUIFramework.Common.Tweening;

namespace SilkyUIFramework.Extensions;

/// <summary>
/// 基于成员名的 Tween 便捷扩展。
/// 这些方法属于扩展层，适合配置化或工具化动画；性能敏感路径优先使用强类型 TweenProperty。
/// </summary>
public static class TweenReflectionExtensions
{
    /// <summary>
    /// 缓存非公开泛型桥接方法，供 object 入口在运行时按成员真实类型闭合泛型参数。
    /// </summary>
    private static readonly MethodInfo _memberToCoreMethod =
        typeof(TweenReflectionExtensions).GetMethod(nameof(MemberToCore), BindingFlags.NonPublic | BindingFlags.Static);

    extension(Tween tween)
    {
        /// <summary>
        /// 使用显式插值函数补间目标对象的实例属性或字段。
        /// </summary>
        /// <typeparam name="TValue">成员值类型，必须与目标成员类型完全一致。</typeparam>
        /// <param name="target">目标对象。</param>
        /// <param name="memberName">目标对象上的实例属性名或字段名。</param>
        /// <param name="to">补间目标值。</param>
        /// <param name="duration">持续时间（秒）。</param>
        /// <param name="lerpFunc">插值函数。</param>
        /// <returns>创建的 Tween 条目，可继续配置缓动、曲线、延迟等。</returns>
        public TweenEntry MemberTo<TValue>(
            object target,
            string memberName,
            TValue to,
            float duration,
            Func<TValue, TValue, float, TValue> lerpFunc)
        {
            ArgumentNullException.ThrowIfNull(tween);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
            ArgumentNullException.ThrowIfNull(lerpFunc);

            var accessor = ObjectAccessorCache.GetAccessor(target);
            var memberType = accessor.GetMemberType(memberName);
            ValidateMemberType<TValue>(target, memberName, memberType);

            // 只在创建条目时解析属性访问器；Tick 热路径复用已编译的 getter/setter 委托。
            var context = new ReflectionPropertyContext<TValue>(
                target,
                accessor.GetGetter(memberName),
                accessor.GetSetter(memberName));

            return tween.TweenProperty(
                context,
                static (ctx, value) => ctx.Set(value),
                static ctx => ctx.Get(),
                to,
                duration,
                lerpFunc);
        }

        /// <summary>
        /// 使用 <see cref="TweenLerpRegistry"/> 中注册的插值函数补间目标对象的实例属性或字段。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="memberName">目标对象上的实例属性名或字段名。</param>
        /// <param name="to">补间目标值，会尝试转换为成员真实类型。</param>
        /// <param name="duration">持续时间（秒）。</param>
        /// <returns>创建的 Tween 条目，可继续配置缓动、曲线、延迟等。</returns>
        public TweenEntry MemberTo(object target, string memberName, object to, float duration)
        {
            ArgumentNullException.ThrowIfNull(tween);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

            var accessor = ObjectAccessorCache.GetAccessor(target);
            var memberType = accessor.GetMemberType(memberName);
            var convertedTo = ConvertToMemberValue(to, memberType, target, memberName);
            var lerpFunc = TweenLerpRegistry.Get(memberType);

            // 这里拿到的成员类型只有运行时 Type，因此通过反射闭合泛型后复用强类型重载。
            return InvokeMemberToCore(tween, target, memberName, convertedTo, duration, lerpFunc, memberType);
        }
    }

    /// <summary>
    /// 按运行时成员类型调用 <see cref="MemberToCore{TValue}"/>。
    /// </summary>
    private static TweenEntry InvokeMemberToCore(
        Tween tween,
        object target,
        string memberName,
        object to,
        float duration,
        Delegate lerpFunc,
        Type memberType)
    {
        return (TweenEntry)_memberToCoreMethod
            .MakeGenericMethod(memberType)
            .Invoke(null, [tween, target, memberName, to, duration, lerpFunc]);
    }

    /// <summary>
    /// 将 object 形式的目标值与插值委托还原为具体成员类型，并转发到显式插值函数重载。
    /// </summary>
    private static TweenEntry MemberToCore<TValue>(
        Tween tween,
        object target,
        string memberName,
        object to,
        float duration,
        Delegate lerpFunc)
    {
        return tween.MemberTo(
            target,
            memberName,
            (TValue)to,
            duration,
            (Func<TValue, TValue, float, TValue>)lerpFunc);
    }

    /// <summary>
    /// 确保显式泛型重载的 TValue 与成员真实类型一致，避免运行到 Tick 时才暴露类型错误。
    /// </summary>
    private static void ValidateMemberType<TValue>(object target, string memberName, Type memberType)
    {
        if (memberType == typeof(TValue)) return;

        throw new InvalidOperationException(
            $"Member '{target.GetType().FullName}.{memberName}' is '{memberType.FullName}', " +
            $"but MemberTo was called with value type '{typeof(TValue).FullName}'.");
    }

    /// <summary>
    /// 将 object 入口传入的目标值整理为成员 setter 可接受的类型。
    /// </summary>
    private static object ConvertToMemberValue(object value, Type memberType, object target, string memberName)
    {
        if (value is null)
        {
            if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) is null)
                throw new InvalidOperationException(
                    $"Cannot assign null to non-nullable member '{target.GetType().FullName}.{memberName}'.");

            return null;
        }

        if (memberType.IsInstanceOfType(value))
            return value;

        var conversionType = Nullable.GetUnderlyingType(memberType) ?? memberType;

        if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(conversionType))
            return Convert.ChangeType(value, conversionType);

        throw new InvalidOperationException(
            $"Cannot convert value of type '{value.GetType().FullName}' to member " +
            $"'{target.GetType().FullName}.{memberName}' of type '{memberType.FullName}'.");
    }

    /// <summary>
    /// 保存一次成员补间所需的目标对象与已编译访问器。
    /// </summary>
    private sealed class ReflectionPropertyContext<TValue>(
        object target,
        Func<object, object> getter,
        Action<object, object> setter)
    {
        private readonly object _target = target;
        private readonly Func<object, object> _getter = getter;
        private readonly Action<object, object> _setter = setter;

        public TValue Get() => (TValue)_getter(_target);

        public void Set(TValue value) => _setter(_target, value);
    }
}
