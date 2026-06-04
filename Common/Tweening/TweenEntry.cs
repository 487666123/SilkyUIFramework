namespace SilkyUIFramework.Common.Tweening;

/// <summary>
/// 缓动方向。
/// </summary>
public enum EaseType
{
    /// <summary>缓入：起始缓慢，末尾加速</summary>
    In = 0,
    /// <summary>缓出：起始快速，末尾减速</summary>
    Out = 1,
    /// <summary>缓入缓出：两端缓慢，中间加速</summary>
    InOut = 2,
    /// <summary>缓出缓入：两端加速，中间缓慢</summary>
    OutIn = 3,
}

/// <summary>
/// Tween 条目的抽象基类。每个条目代表一个可被编排的动画单元（属性插值或回调）。
/// </summary>
public abstract class TweenEntry
{
    private float _duration;
    private float _delay;
    private EaseType _easeType = EaseType.InOut;
    private TransitionType _transitionType = TransitionType.Linear;

    /// <summary>动画持续时间（秒）</summary>
    public float Duration
    {
        get => _duration;
        set => _duration = Math.Clamp(value, 0f, float.MaxValue);
    }

    /// <summary>延迟时间（秒），到期后才开始执行</summary>
    public float Delay
    {
        get => _delay;
        set => _delay = Math.Clamp(value, 0f, float.MaxValue);
    }

    /// <summary>已累计的时间（内部使用）</summary>
    internal float Elapsed;

    /// <summary>是否已完成（内部使用）</summary>
    internal bool IsCompleted;

    /// <summary>每帧调用，由 Tween.Update 驱动</summary>
    internal abstract void Tick(float delta);

    /// <summary>重置条目到初始状态</summary>
    internal virtual void Reset()
    {
        Elapsed = 0;
        IsCompleted = false;
    }

    // ────────────────────── Fluent 配置 ──────────────────────

    /// <summary>设置缓动方向</summary>
    public TweenEntry SetEase(EaseType t)
    {
        _easeType = t;
        return this;
    }

    /// <summary>设置过渡曲线</summary>
    public TweenEntry SetTrans(TransitionType t)
    {
        _transitionType = t;
        return this;
    }

    /// <summary>设置延迟时间。负数会被限制为 0。</summary>
    public TweenEntry SetDelay(float d)
    {
        Delay = d;
        return this;
    }

    /// <summary>设置持续时间。负数会被限制为 0。</summary>
    public TweenEntry SetDuration(float d)
    {
        Duration = d;
        return this;
    }

    // ────────────────────── 缓动组合 ──────────────────────

    /// <summary>
    /// 将 <see cref="TransitionType"/> 的曲线函数与 <see cref="EaseType"/> 的方向组合，
    /// 产生最终的缓动输出。
    /// <code>
    /// In    → curve(t)
    /// Out   → 1 − curve(1 − t)
    /// InOut → t&lt;0.5 ? curve(2t)·0.5 : 1−curve(2−2t)·0.5
    /// OutIn → t&lt;0.5 ? (1−curve(1−2t))·0.5 : 0.5+curve(2t−1)·0.5
    /// </code>
    /// </summary>
    /// <param name="t">归一化时间 [0,1]</param>
    /// <param name="transType">过渡曲线类型</param>
    /// <param name="easeType">缓动方向</param>
    /// <returns>缓动后的输出值 [0,1]</returns>
    internal static float ApplyEasing(float t, TransitionType transType, EaseType easeType)
    {
        if (transType == TransitionType.Linear) return t;

        var curve = Transition.Get(transType);
        return easeType switch
        {
            EaseType.In => curve(t),
            EaseType.Out => 1 - curve(1 - t),
            EaseType.InOut => t < 0.5f
                ? curve(2 * t) * 0.5f
                : 1 - curve(2 - 2 * t) * 0.5f,
            EaseType.OutIn => t < 0.5f
                ? (1 - curve(1 - 2 * t)) * 0.5f
                : 0.5f + curve(2 * t - 1) * 0.5f,
            _ => t,
        };
    }

    protected float ApplyEasing(float t) => ApplyEasing(t, _transitionType, _easeType);
}
