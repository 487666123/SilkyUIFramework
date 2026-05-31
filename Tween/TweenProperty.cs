using System;

namespace SilkyUIFramework.Tween;

/// <summary>
/// 属性插值条目。由 <see cref="Tween.TweenProperty{T}"/> 内部创建，用户不直接实例化。
/// 延迟到期时通过 getter 读取当前值作为起始值，Tick 热路径零分配。
/// </summary>
internal sealed class TweenProperty : TweenEntry
{
    /// <summary>起始值获取委托（已装箱），延迟到期时调用一次</summary>
    private readonly Func<object> _getter;

    /// <summary>插值目标值（装箱）</summary>
    private readonly object _to;

    /// <summary>属性赋值委托（已装箱）</summary>
    private readonly Action<object> _setter;

    /// <summary>插值函数委托（已装箱），签名 object(object from, object to, float t)</summary>
    private readonly Func<object, object, float, object> _lerpFunc;

    /// <summary>延迟到期后捕获的起始值</summary>
    private object _from = null!;

    /// <summary>是否已从 getter 捕获起始值</summary>
    private bool _fromCaptured;

    private TweenProperty(
        Func<object> getter, object to, float duration,
        Action<object> setter, Func<object, object, float, object> lerpFunc)
    {
        _getter = getter;
        _to = to;
        Duration = duration;
        _setter = setter;
        _lerpFunc = lerpFunc;
    }

    /// <summary>
    /// 创建属性插值条目。泛型参数在调用点推断，生成的闭包捕获具体类型，
    /// 装箱仅发生在 Create 和首次 Tick，后续 Tick 无额外分配。
    /// </summary>
    internal static TweenProperty Create<T>(
        Action<T> setter, Func<T> getter, T to, float duration,
        Func<T, T, float, T> lerpFunc)
    {
        return new TweenProperty(
            () => getter()!, to!, duration,
            v => setter((T)v),
            (a, b, t) => lerpFunc((T)a, (T)b, t)!);
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
