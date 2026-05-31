using System;
using System.Collections.Generic;

namespace SilkyUIFramework.Tween;

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
    /// <summary>已完成（所有循环结束）</summary>
    Completed = 3,
    /// <summary>被手动停止</summary>
    Stopped = 4,
}

/// <summary>
/// Tween 编排器。Step 间顺序执行，Step 内并行执行。
/// <example>
/// 基本用法：
/// <code>
/// var tween = new Tween();
/// tween.TweenProperty&lt;float&gt;(v => obj.X = v, 0f, 100f, 0.5f, (a, b, t) => a + (b - a) * t)
///      .SetEase(EaseType.Out).SetTrans(TransitionType.Quad);
/// tween.Play();
/// // 每帧调用:
/// tween.Update(deltaSeconds);
/// </code>
/// </example>
/// </summary>
public class Tween : IDisposable
{
    private sealed class TweenStep
    {
        /// <summary>Step 内的条目列表，同时并行执行</summary>
        public readonly List<TweenEntry> Entries = [];

        /// <summary>Step 是否已全部执行完毕</summary>
        public bool Completed;
    }

    #region 字段

    /// <summary>所有编排步骤</summary>
    private readonly List<TweenStep> _steps = [];

    /// <summary>当前正在执行的 Step 索引</summary>
    private int _currentStepIndex;

    /// <summary>并行模式标记：true 时后续条目加入当前 Step</summary>
    private bool _isParallel;

    /// <summary>总循环次数（1=默认，-1=无限）</summary>
    private int _totalLoops = 1;

    /// <summary>已完成的循环次数</summary>
    private int _loopsCompleted;

    #endregion

    #region 属性

    /// <summary>当前生命周期状态</summary>
    public TweenState State { get; private set; } = TweenState.Idle;

    /// <summary>是否正在播放</summary>
    public bool IsPlaying => State == TweenState.Playing;

    /// <summary>本次播放累计时间（不含已完成循环的时间）</summary>
    public float TotalElapsed { get; private set; }

    #endregion

    #region 事件

    /// <summary>所有循环完成时触发</summary>
    public event Action OnCompleted;

    /// <summary>被 Stop() 停止时触发</summary>
    public event Action OnStopped;

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
    /// <typeparam name="T">属性值类型</typeparam>
    /// <param name="setter">属性赋值委托</param>
    /// <param name="getter">起始值获取委托，延迟到期时调用一次</param>
    /// <param name="to">目标值</param>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="lerpFunc">插值函数，如 <c>(a, b, t) => a + (b - a) * t</c></param>
    /// <returns>创建的条目，支持链式配置</returns>
    public TweenEntry TweenProperty<T>(
        Action<T> setter, Func<T> getter, T to, float duration,
        Func<T, T, float, T> lerpFunc)
    {
        var entry = global::SilkyUIFramework.Tween.TweenProperty.Create(setter, getter, to, duration, lerpFunc);
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
    /// <param name="count">1 = 默认（播放一次），-1 = 无限循环</param>
    public Tween SetLoops(int count)
    {
        _totalLoops = count;
        return this;
    }

    #endregion

    #region 生命周期控制

    /// <summary>
    /// 开始播放。从 Idle/Completed/Stopped 状态调用会重置并重新开始；
    /// 从 Paused 状态调用会恢复播放。
    /// </summary>
    public void Play()
    {
        switch (State)
        {
            case TweenState.Playing:
                return;
            case TweenState.Paused:
                State = TweenState.Playing;
                return;
            default:
                // Idle、Completed、Stopped → 重置后开始
                ResetSteps();
                _loopsCompleted = 0;
                TotalElapsed = 0;
                State = TweenState.Playing;
                break;
        }
    }

    /// <summary>暂停播放。之后可用 <see cref="Resume"/> 或 <see cref="Play"/> 继续。</summary>
    public void Pause()
    {
        if (State == TweenState.Playing)
            State = TweenState.Paused;
    }

    /// <summary>从暂停状态恢复播放。</summary>
    public void Resume()
    {
        if (State == TweenState.Paused)
            State = TweenState.Playing;
    }

    /// <summary>
    /// 停止播放。保留当前属性值不变（不 snap 到终点）。
    /// 停止后可通过 <see cref="Play"/> 重新开始。
    /// </summary>
    public void Stop()
    {
        if (State is TweenState.Playing or TweenState.Paused)
        {
            State = TweenState.Stopped;
            OnStopped?.Invoke();
        }
    }

    /// <summary>
    /// 完全重置所有状态。清空累计时间、循环计数，回到 Idle。
    /// 之后需要调用 <see cref="Play"/> 重新开始。
    /// </summary>
    public void Reset()
    {
        State = TweenState.Idle;
        ResetSteps();
        _loopsCompleted = 0;
        TotalElapsed = 0;
    }

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

        TotalElapsed += deltaSeconds;

        // 全部 Step 已完成
        if (_currentStepIndex >= _steps.Count)
        {
            HandleLoopOrComplete();
            return;
        }

        var step = _steps[_currentStepIndex];
        bool allDone = true;

        foreach (var entry in step.Entries)
        {
            if (!entry.IsCompleted)
                entry.Tick(deltaSeconds);
            if (!entry.IsCompleted)
                allDone = false;
        }

        if (allDone)
        {
            step.Completed = true;
            _currentStepIndex++;

            if (_currentStepIndex >= _steps.Count)
                HandleLoopOrComplete();
        }
    }

    #endregion

    #region 内部方法

    private void HandleLoopOrComplete()
    {
        _loopsCompleted++;

        bool shouldLoop = _totalLoops == -1 || _loopsCompleted < _totalLoops;

        if (shouldLoop)
        {
            ResetSteps();
            _currentStepIndex = 0;
        }
        else
        {
            Complete();
        }
    }

    private void Complete()
    {
        State = TweenState.Completed;
        OnCompleted?.Invoke();
    }

    private void ResetSteps()
    {
        _currentStepIndex = 0;
        foreach (var step in _steps)
        {
            step.Completed = false;
            foreach (var entry in step.Entries)
                entry.Reset();
        }
    }

    #endregion

    #region 释放

    /// <summary>
    /// 释放事件引用，防止 GC 无法回收。
    /// 调用后 Tween 仍可使用，但事件已清空。
    /// </summary>
#pragma warning disable CA1816 // Dispose 方法应调用 SuppressFinalize
    void IDisposable.Dispose()
#pragma warning restore CA1816 // Dispose 方法应调用 SuppressFinalize
    {
        OnCompleted = null;
        OnStopped = null;
    }

    #endregion
}
