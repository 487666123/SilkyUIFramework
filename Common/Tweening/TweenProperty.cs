using System;

namespace SilkyUIFramework.Common.Tweening;

/// <summary>
/// 属性插值条目。由 <see cref="Tween.TweenProperty{T}"/> 内部创建，用户不直接实例化。
/// 延迟到期时通过 getter 读取当前值作为起始值，Tick 热路径零分配。
/// </summary>
internal static class TweenProperty
{
    /// <summary>
    /// 创建属性插值条目。泛型参数在调用点推断，后续 Tick 使用具体类型字段，避免值类型每帧装箱。
    /// </summary>
    internal static TweenProperty<T> Create<T>(
        Action<T> setter, Func<T> getter, T to, float duration,
        Func<T, T, float, T> lerpFunc)
    {
        return new TweenProperty<T>(setter, getter, to, duration, lerpFunc);
    }
}

internal sealed class TweenProperty<T> : TweenEntry
{
    /// <summary>起始值获取委托，延迟到期时调用一次</summary>
    private readonly Func<T> _getter;

    /// <summary>插值目标值</summary>
    private readonly T _to;

    /// <summary>属性赋值委托</summary>
    private readonly Action<T> _setter;

    /// <summary>插值函数委托</summary>
    private readonly Func<T, T, float, T> _lerpFunc;

    /// <summary>延迟到期后捕获的起始值</summary>
    private T _from = default!;

    /// <summary>是否已从 getter 捕获起始值</summary>
    private bool _fromCaptured;

    internal TweenProperty(
        Action<T> setter, Func<T> getter, T to, float duration,
        Func<T, T, float, T> lerpFunc)
    {
        _setter = setter;
        _getter = getter;
        _to = to;
        Duration = duration;
        _lerpFunc = lerpFunc;
    }

    internal override void Tick(float delta)
    {
        if (IsCompleted) return;

        Elapsed += delta;
        if (Elapsed < Delay) return;

        if (!_fromCaptured)
        {
            _from = _getter();
            _fromCaptured = true;
        }

        if (Duration <= 0f)
        {
            _setter(_to);
            IsCompleted = true;
            return;
        }

        float rawT = Math.Clamp((Elapsed - Delay) / Duration, 0f, 1f);

        if (rawT >= 1f)
        {
            _setter(_to);
            IsCompleted = true;
            return;
        }

        float easedT = ApplyEasing(rawT, TransitionType, EaseType);
        _setter(_lerpFunc(_from, _to, easedT));
    }

    internal override void Reset()
    {
        base.Reset();
        _fromCaptured = false;
    }
}
