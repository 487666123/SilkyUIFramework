using System;

namespace SilkyUIFramework.Common.Tweening;

/// <summary>
/// 属性插值条目。由 <see cref="Tween.TweenProperty{TTarget,TValue}"/> 内部创建，用户不直接实例化。
/// 延迟到期时通过 getter 读取当前值作为起始值，Tick 热路径零分配。
/// </summary>
internal static class TweenProperty
{
    /// <summary>
    /// 创建属性插值条目。泛型参数在调用点推断，后续 Tick 使用具体类型字段，避免值类型每帧装箱。
    /// </summary>
    internal static TweenProperty<TTarget, TValue> Create<TTarget, TValue>(
        TTarget target,
        Action<TTarget, TValue> setter,
        Func<TTarget, TValue> getter,
        TValue to,
        float duration,
        Func<TValue, TValue, float, TValue> lerpFunc)
    {
        return new TweenProperty<TTarget, TValue>(target, setter, getter, to, duration, lerpFunc);
    }
}

internal sealed class TweenProperty<TTarget, TValue> : TweenEntry
{
    /// <summary>属性所属目标。可为 null，用于无目标的委托式补间。</summary>
    private readonly TTarget _target;

    /// <summary>起始值获取委托，延迟到期时调用一次</summary>
    private readonly Func<TTarget, TValue> _getter;

    /// <summary>插值目标值</summary>
    private readonly TValue _to;

    /// <summary>属性赋值委托</summary>
    private readonly Action<TTarget, TValue> _setter;

    /// <summary>插值函数委托</summary>
    private readonly Func<TValue, TValue, float, TValue> _lerpFunc;

    /// <summary>延迟到期后捕获的起始值</summary>
    private TValue _from = default!;

    /// <summary>是否已从 getter 捕获起始值</summary>
    private bool _fromCaptured;

    /// <summary>当前循环是否反向播放</summary>
    private bool _reverse;

    internal TweenProperty(
        TTarget target,
        Action<TTarget, TValue> setter,
        Func<TTarget, TValue> getter,
        TValue to,
        float duration,
        Func<TValue, TValue, float, TValue> lerpFunc)
    {
        _target = target;
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
            _from = _getter(_target);
            _fromCaptured = true;
        }

        TValue from = _reverse ? _to : _from;
        TValue to = _reverse ? _from : _to;

        if (Duration <= 0f)
        {
            _setter(_target, to);
            IsCompleted = true;
            return;
        }

        float rawT = Math.Clamp((Elapsed - Delay) / Duration, 0f, 1f);

        if (rawT >= 1f)
        {
            _setter(_target, to);
            IsCompleted = true;
            return;
        }

        float easedT = ApplyEasing(rawT);
        _setter(_target, _lerpFunc(from, to, easedT));
    }

    internal override void Reset(bool reverse)
    {
        base.Reset(reverse);
        _reverse = reverse;

        if (_fromCaptured)
            _setter(_target, reverse ? _to : _from);
    }
}
