using SilkyUIFramework.Common.Reflection;
using SilkyUIFramework.Common.Tweening;
using SilkyUIFramework.Extensions.Tweening;

namespace SilkyUIFramework.StyleSystem;

/// <summary>
/// 可共享的样式目标值。通过泛型实现保留值类型，不保存绑定对象或动画运行状态。
/// </summary>
public abstract class StyleValue
{
    private protected StyleValue() { }

    /// <summary>包装目标值，供样式索引器和批量设置使用；T 必须与成员声明类型完全一致。</summary>
    public static StyleValue<T> Create<T>(T value) => new(value);

    /// <summary>查询当前注册表，不在共享样式值中缓存插值函数。</summary>
    internal abstract bool CanTween { get; }

    /// <summary>校验成员类型并读取当前值，使用强类型比较判断是否已达到目标。</summary>
    internal abstract bool IsCurrentValue(ObjectAccessor accessor, object owner, string memberName);

    /// <summary>直接将目标值写入成员。</summary>
    internal abstract void SetValue(ObjectAccessor accessor, object owner, string memberName);

    /// <summary>向已有动画添加强类型补间条目，延迟结束后才读取起点。</summary>
    internal abstract TweenEntry CreateTween(Tween tween, ObjectAccessor accessor, object owner,
        string memberName, float duration);
}

/// <summary>
/// 不可变的强类型样式值。比较、赋值和补间均保留 T，不做字符串或数值类型转换。
/// </summary>
/// <typeparam name="T">成员声明类型，包括可空值类型、基类或接口；必须与成员类型完全一致。</typeparam>
public sealed class StyleValue<T>(T value) : StyleValue
{
    public T Value { get; } = value;

    internal override bool CanTween => TweenLerpRegistry.TryGet<T>(out _);

    internal override bool IsCurrentValue(ObjectAccessor accessor, object owner, string memberName)
    {
        var getter = accessor.GetTypedGetter<object, T>(memberName);
        return EqualityComparer<T>.Default.Equals(getter(owner), Value);
    }

    internal override void SetValue(ObjectAccessor accessor, object owner, string memberName)
    {
        accessor.GetTypedSetter<object, T>(memberName)(owner, Value);
    }

    internal override TweenEntry CreateTween(Tween tween, ObjectAccessor accessor, object owner,
        string memberName, float duration)
    {
        return tween.TweenProperty(
            owner,
            accessor.GetTypedSetter<object, T>(memberName),
            accessor.GetTypedGetter<object, T>(memberName),
            Value,
            duration,
            TweenLerpRegistry.Get<T>());
    }
}
