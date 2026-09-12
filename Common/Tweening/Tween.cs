using System;
using System.Collections.Generic;
using System.Linq;

namespace SilkyUIFramework.Common.Tweening;

/// <summary>
/// Tween 生命周期状态。
/// </summary>
public enum TweenState
{
    /// <summary>初始状态，尚未开始</summary>
    Idle = 0,
    /// <summary>正在播放</summary>
    Playing = 1,
    /// <summary>已暂停</summary>
    Paused = 2,
    /// <summary>已结束（自然完成或被 Kill），不可恢复</summary>
    Finished = 3,
}

/// <summary>
/// Tween 的循环模式。
/// </summary>
public enum LoopType
{
    /// <summary>每轮从起点重新正向播放</summary>
    Restart = 0,
    /// <summary>每轮交替正向和反向播放</summary>
    Yoyo = 1,
}

/// <summary>
/// Tween 编排器。Step 间顺序执行，Step 内并行执行。
/// <example>
/// 基本用法：
/// <code>
/// var tween = new Tween();
/// tween.TweenProperty(obj, static (target, value) => target.X = value, static target => target.X,
///     100f, 0.5f, static (a, b, t) => a + (b - a) * t)
///      .SetEase(EaseType.Out).SetTrans(TransitionType.Quad);
/// tween.Play();
/// // 每帧调用:
/// tween.Update(deltaSeconds);
/// </code>
/// </example>
/// </summary>
public class Tween
{
    private sealed class TweenStep
    {
        /// <summary>Step 内的条目列表，同时并行执行</summary>
        public readonly List<TweenEntry> Entries = [];
    }

    #region 字段

    /// <summary>所有编排步骤</summary>
    private readonly List<TweenStep> _steps = [];

    /// <summary>当前正在执行的 Step 索引</summary>
    private int _currentStepIndex;

    /// <summary>当前 Step 已消耗的时间</summary>
    private float _currentStepElapsed;

    /// <summary>并行模式标记：true 时后续条目加入当前 Step</summary>
    private bool _isParallel;

    /// <summary>总循环次数（1=默认，-1=无限）</summary>
    private int _loopCount = 1;

    /// <summary>已完成的循环次数</summary>
    private int _completedLoops;

    /// <summary>循环模式</summary>
    private LoopType _loopType = LoopType.Restart;

    /// <summary>当前循环是否反向执行</summary>
    private bool _isReversed;

    /// <summary>后续添加条目的默认缓动方向</summary>
    private EaseType _defaultEaseType = EaseType.InOut;

    /// <summary>后续添加条目的默认过渡曲线类型</summary>
    private TransitionType _defaultTransitionType = TransitionType.Linear;

    #endregion

    #region 属性

    /// <summary>当前生命周期状态</summary>
    public TweenState State { get; private set; } = TweenState.Idle;

    /// <summary>是否正在播放</summary>
    public bool IsPlaying => State == TweenState.Playing;

    /// <summary>是否已结束（自然完成或被 Kill）</summary>
    public bool IsFinished => State == TweenState.Finished;

    /// <summary>
    /// 有效性检查委托。返回 false 时 <see cref="TweenManager"/> 会自动 Kill 此 Tween。
    /// 用于绑定 UI 元素生命周期（如 <c>() => view.SilkyUI != null</c>）。
    /// </summary>
    public Func<bool> ValidityCheck { get; set; }

    #endregion

    #region 事件

    /// <summary>所有循环自然播放完成时触发，先于 OnFinished；Kill 不触发。</summary>
    public event Action OnCompleted;

    /// <summary>被 Kill 终止时触发，先于 OnFinished；自然完成不触发。</summary>
    public event Action OnKilled;

    /// <summary>自然完成或 Kill 均触发，在对应的结束原因事件之后执行，最多触发一次。</summary>
    public event Action OnFinished;

    #endregion

    #region 默认条目配置

    /// <summary>
    /// 设置后续添加条目的默认缓动方向。已添加的条目不受影响。
    /// </summary>
    public Tween SetEase(EaseType easeType)
    {
        _defaultEaseType = easeType;
        return this;
    }

    /// <summary>
    /// 设置后续添加条目的默认过渡曲线类型。已添加的条目不受影响。
    /// </summary>
    public Tween SetTrans(TransitionType transitionType)
    {
        _defaultTransitionType = transitionType;
        return this;
    }

    #endregion

    #region 模式切换

    /// <summary>
    /// 切换到并行模式。后续添加的条目将进入同一个新 Step，同时执行。
    /// 调用 <see cref="Sequential"/> 可切换回顺序模式。
    /// </summary>
    public Tween Parallel()
    {
        _isParallel = true;

        // 仅在需要时创建新 Step：没有 Step 或最后一个 Step 已有条目
        if (_steps.Count == 0 || _steps[^1].Entries.Count > 0)
            _steps.Add(new TweenStep());

        return this;
    }

    /// <summary>
    /// 切换到顺序模式。后续添加的条目各自进入独立 Step，依次执行。
    /// 这是默认模式。
    /// </summary>
    public Tween Sequential()
    {
        _isParallel = false;
        return this;
    }

    #endregion

    #region 添加条目

    /// <summary>
    /// 添加属性插值动画。延迟到期时通过 <paramref name="getter"/> 读取当前值作为起始值，
    /// 随后每帧插值到 <paramref name="to"/>。
    /// </summary>
    /// <typeparam name="TTarget">属性所属目标类型。无目标场景可传入 <see langword="null"/> 目标。</typeparam>
    /// <typeparam name="TValue">属性值类型</typeparam>
    /// <param name="target">属性所属目标，传递给 <paramref name="setter"/> 与 <paramref name="getter"/>。</param>
    /// <param name="setter">属性赋值委托</param>
    /// <param name="getter">起始值获取委托，延迟到期时调用一次</param>
    /// <param name="to">目标值</param>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="lerpFunc">插值函数，如 <c>(a, b, t) => a + (b - a) * t</c></param>
    /// <returns>创建的条目，支持链式配置</returns>
    public TweenEntry TweenProperty<TTarget, TValue>(
        TTarget target,
        Action<TTarget, TValue> setter,
        Func<TTarget, TValue> getter,
        TValue to,
        float duration,
        Func<TValue, TValue, float, TValue> lerpFunc)
    {
        var entry = Tweening.TweenProperty.Create(target, setter, getter, to, duration, lerpFunc);
        AddEntry(entry);
        return entry;
    }

    /// <summary>
    /// 添加回调。延迟到期后触发一次即完成。
    /// </summary>
    /// <param name="callback">回调委托</param>
    /// <returns>创建的条目，支持链式配置（如 SetDelay）</returns>
    public TweenEntry TweenCallback(Action callback)
    {
        var entry = new TweenCallback(callback);
        AddEntry(entry);
        return entry;
    }

    private void AddEntry(TweenEntry entry)
    {
        entry.SetTrans(_defaultTransitionType).SetEase(_defaultEaseType);

        if (_isParallel && _steps.Count > 0)
            _steps[^1].Entries.Add(entry);
        else
            _steps.Add(new TweenStep { Entries = { entry } });
    }

    #endregion

    #region 循环配置

    /// <summary>
    /// 设置循环次数。
    /// </summary>
    /// <param name="count">1 = 默认（播放一次），-1 = 无限循环；其他值必须大于 0</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="count"/> 不是 -1 且小于 1 时抛出。</exception>
    public Tween SetLoops(int count)
    {
        if (count != -1 && count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Loop count must be -1 or greater than 0.");

        _loopCount = count;
        return this;
    }

    /// <summary>设置循环模式。默认是每轮从起点重新正向播放。</summary>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="loopType"/> 不是已知模式时抛出。</exception>
    public Tween SetLoopType(LoopType loopType)
    {
        if (loopType is not (LoopType.Restart or LoopType.Yoyo))
            throw new ArgumentOutOfRangeException(nameof(loopType), loopType, "Unknown loop type.");

        _loopType = loopType;
        return this;
    }

    #endregion

    #region 生命周期控制

    /// <summary>
    /// 开始播放。仅从 Idle 或 Paused 状态有效。
    /// 如果没有任何条目，下一次 Update 会直接进入 Finished。
    /// </summary>
    public void Play()
    {
        switch (State)
        {
            case TweenState.Playing:
            case TweenState.Finished:
                return;
            case TweenState.Paused:
                State = TweenState.Playing;
                return;
            default: // Idle
                State = TweenState.Playing;
                break;
        }
    }

    /// <summary>暂停播放。之后可用 <see cref="Play"/> 恢复。</summary>
    public void Pause()
    {
        if (State == TweenState.Playing)
            State = TweenState.Paused;
    }

    /// <summary>
    /// 终止尚未结束的 Tween，进入 <see cref="TweenState.Finished"/>。
    /// 依次触发 <see cref="OnKilled"/> 和 <see cref="OnFinished"/>，已结束时不重复触发。
    /// <see cref="TweenManager"/> 会在下一帧自动回收。
    /// </summary>
    public void Kill() => Finish(completed: false);

    #endregion

    #region 每帧更新

    /// <summary>
    /// 驱动 Tween 的每帧更新。调用者应在自己的更新循环中传入 <c>deltaSeconds</c>。
    /// </summary>
    /// <param name="deltaSeconds">距上一帧的时间（秒）</param>
    public void Update(float deltaSeconds)
    {
        if (State != TweenState.Playing)
            return;

        deltaSeconds = Math.Clamp(deltaSeconds, 0f, float.MaxValue);

        if (_steps.Count == 0)
        {
            AdvanceLoop();
            return;
        }

        float remaining = deltaSeconds;
        int zeroProgressSteps = 0;

        while (State == TweenState.Playing)
        {
            if (_currentStepIndex < 0 || _currentStepIndex >= _steps.Count)
            {
                if (!AdvanceLoop() || remaining <= 0f)
                    return;

                continue;
            }

            var step = _steps[_currentStepIndex];
            float stepDuration = GetStepDuration(step);
            float stepRemaining = MathF.Max(0f, stepDuration - _currentStepElapsed);
            float tickDelta = MathF.Min(remaining, stepRemaining);
            var allDone = true;

            foreach (var entry in step.Entries)
            {
                if (!entry.IsCompleted)
                    entry.Tick(tickDelta);
                if (State != TweenState.Playing)
                    return;
                if (!entry.IsCompleted)
                    allDone = false;
            }

            _currentStepElapsed += tickDelta;

            if (!allDone)
                return;

            remaining -= tickDelta;
            if (tickDelta <= 0f)
            {
                zeroProgressSteps++;
                if (zeroProgressSteps > _steps.Count)
                    return;
            }
            else
            {
                zeroProgressSteps = 0;
            }

            if (!MoveToNextStep() || remaining <= 0f)
                return;
        }
    }

    #endregion

    #region 内部方法

    /// <summary>统一结束入口：先固定终止状态，再通知结束原因和通用清理事件。</summary>
    private void Finish(bool completed)
    {
        if (IsFinished) return;

        // 回调中再次调用 Kill、Play 或 Update，也不会重新结束或恢复此 Tween。
        State = TweenState.Finished;
        try
        {
            if (completed) OnCompleted?.Invoke();
            else OnKilled?.Invoke();
        }
        finally
        {
            // 即使结束原因的回调抛出异常，也保留原有的通用清理通知。
            OnFinished?.Invoke();
        }
    }

    private static float GetStepDuration(TweenStep step)
    {
        float duration = 0f;
        foreach (var entry in step.Entries)
        {
            if (entry.TotalDuration > duration)
                duration = entry.TotalDuration;
        }

        return duration;
    }

    private bool MoveToNextStep()
    {
        _currentStepElapsed = 0f;
        _currentStepIndex += _isReversed ? -1 : 1;

        if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
            return true;

        return AdvanceLoop();
    }

    /// <summary>推进循环，并准备下一轮的播放方向。</summary>
    private bool AdvanceLoop()
    {
        _completedLoops++;

        if (_loopCount != -1 && _completedLoops >= _loopCount)
        {
            Finish(completed: true);
            return false;
        }

        bool reverse = _loopType == LoopType.Yoyo && !_isReversed;
        ResetSteps(reverse);
        return true;
    }

    private void ResetSteps(bool reverse)
    {
        _currentStepIndex = reverse ? _steps.Count - 1 : 0;
        _currentStepElapsed = 0f;
        _isReversed = reverse;

        foreach (var entry in _steps.SelectMany(s => s.Entries))
            entry.Reset(reverse);
    }

    #endregion
}
