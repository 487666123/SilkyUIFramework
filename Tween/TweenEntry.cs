namespace SilkyUIFramework.Tween;

/// <summary>
/// Tween 条目的抽象基类。每个条目代表一个可被编排的动画单元（属性插值或回调）。
/// </summary>
public abstract class TweenEntry
{
    /// <summary>动画持续时间（秒）</summary>
    public float Duration { get; set; }

    /// <summary>延迟时间（秒），到期后才开始执行</summary>
    public float Delay { get; set; }

    /// <summary>缓动方向</summary>
    public EaseType EaseType { get; set; } = EaseType.InOut;

    /// <summary>过渡曲线类型</summary>
    public TransitionType TransitionType { get; set; } = TransitionType.Linear;

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
        EaseType = t;
        return this;
    }

    /// <summary>设置过渡曲线</summary>
    public TweenEntry SetTrans(TransitionType t)
    {
        TransitionType = t;
        return this;
    }

    /// <summary>设置延迟时间</summary>
    public TweenEntry SetDelay(float d)
    {
        Delay = d;
        return this;
    }

    /// <summary>设置持续时间</summary>
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
    /// <param name="trans">过渡曲线类型</param>
    /// <param name="ease">缓动方向</param>
    /// <returns>缓动后的输出值 [0,1]</returns>
    internal static float ApplyEasing(float t, TransitionType trans, EaseType ease)
    {
        var curve = Transition.Map[(int)trans];
        return ease switch
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
}
