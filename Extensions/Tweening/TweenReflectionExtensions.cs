using SilkyUIFramework.Common.Reflection;
using SilkyUIFramework.Common.Tweening;
using SilkyUIFramework.Extensions.Tweening;

namespace SilkyUIFramework.Extensions;

/// <summary>
/// 基于成员名的 Tween 便捷扩展。
/// 这些方法属于扩展层，适合配置化或工具化动画；性能敏感路径优先使用强类型 TweenProperty。
/// </summary>
public static class TweenReflectionExtensions
{
    extension(Tween tween)
    {
        /// <summary>
        /// 按对象的运行时类型绑定单个实例成员，不解析嵌套路径。
        /// 对象和值以 object 传入时使用此入口；动画始终操作创建时的对象，延迟到期才读取起点。
        /// 值类型通过此重载插值会装箱；已知类型的调用仍可使用泛型重载。
        /// </summary>
        public TweenEntry MemberTo(object target, string memberName, object to, float duration)
        {
            ArgumentNullException.ThrowIfNull(tween);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

            var accessor = ObjectAccessorCache.GetAccessor(target);
            var memberType = accessor.GetMemberType(memberName);
            var underlyingType = Nullable.GetUnderlyingType(memberType);
            var compatible = to == null
                ? underlyingType != null || !memberType.IsValueType
                : (underlyingType ?? memberType).IsInstanceOfType(to);
            if (!compatible)
                throw new ArgumentException($"目标值不能赋给成员 '{target.GetType().FullName}.{memberName}' ({memberType.FullName})。", nameof(to));

            if (!TweenLerpRegistry.TryGet(memberType, out var interpolate))
                throw TweenLerpRegistry.CreateMissingLerpException(memberType);

            return tween.TweenProperty(target, accessor.GetSetter(memberName), accessor.GetGetter(memberName),
                to, duration, interpolate);
        }

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
        public TweenEntry MemberTo<TTarget, TValue>(
            TTarget target,
            string memberName,
            TValue to,
            float duration,
            Func<TValue, TValue, float, TValue> lerpFunc)
        {
            ArgumentNullException.ThrowIfNull(tween);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
            ArgumentNullException.ThrowIfNull(lerpFunc);

            var accessor = ObjectAccessorCache.GetAccessor<TTarget>();
            var memberType = accessor.GetMemberType(memberName);
            ValidateMemberType<TValue>(target, memberName, memberType);

            // 只在创建条目时解析访问器；Tick 热路径复用强类型委托，避免值类型装箱。
            return tween.TweenProperty(
                target,
                accessor.GetTypedSetter<TTarget, TValue>(memberName),
                accessor.GetTypedGetter<TTarget, TValue>(memberName),
                to,
                duration,
                lerpFunc);
        }

        /// <summary>
        /// 使用 <see cref="TweenLerpRegistry"/> 中注册的插值函数补间目标对象的实例属性或字段。
        /// </summary>
        /// <typeparam name="TTarget">目标对象类型。</typeparam>
        /// <typeparam name="TValue">成员值类型，必须与目标成员类型完全一致。</typeparam>
        /// <param name="target">目标对象。</param>
        /// <param name="memberName">目标对象上的实例属性名或字段名。</param>
        /// <param name="to">补间目标值。</param>
        /// <param name="duration">持续时间（秒）。</param>
        /// <returns>创建的 Tween 条目，可继续配置缓动、曲线、延迟等。</returns>
        public TweenEntry MemberTo<TTarget, TValue>(TTarget target, string memberName, TValue to, float duration)
        {
            ArgumentNullException.ThrowIfNull(tween);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

            var accessor = ObjectAccessorCache.GetAccessor<TTarget>();
            var memberType = accessor.GetMemberType(memberName);
            ValidateMemberType<TValue>(target, memberName, memberType);

            var lerpFunc = TweenLerpRegistry.Get<TValue>();

            return tween.TweenProperty(
                target,
                accessor.GetTypedSetter<TTarget, TValue>(memberName),
                accessor.GetTypedGetter<TTarget, TValue>(memberName),
                to,
                duration,
                lerpFunc);
        }
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
}
