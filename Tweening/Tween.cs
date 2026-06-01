using System;
using System.Collections.Generic;
using System.Linq;

namespace SilkyUIFramework.Tweening;

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

    /// <summary>并行模式标记：true 时后续条目加入当前 Step</summary>
    private bool _isParallel;

    /// <summary>总循环次数（1=默认，-1=无限）</summary>
    private int _loopCount = 1;

    /// <summary>已完成的循环次数</summary>
    private int _completedLoops;

    #endregion

    #region 属性

    /// <summary>当前生命周期状态</summary>
    public TweenState State { get; private set; } = TweenState.Idle;

    /// <summary>是否正在播放</summary>
    public bool IsPlaying => State == TweenState.Playing;

    /// <summary>是否已结束</summary>
    public bool IsFinished => State == TweenState.Finished;

    /// <summary>
    /// 有效性检查委托。返回 false 时 <see cref="TweenManager"/> 会自动 Kill 此 Tween。
    /// 用于绑定 UI 元素生命周期（如 <c>() => view.SilkyUI != null</c>）。
    /// </summary>
    public Func<bool> ValidityCheck { get; set; }

    #endregion

    #region 事件

    /// <summary>进入 Finished 状态时触发（自然完成或 Kill 均触发）</summary>
    public event Action OnFinished;

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
        var entry = Tweening.TweenProperty.Create(setter, getter, to, duration, lerpFunc);
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
    /// <param name="count">1 = 默认（播放一次），-1 = 无限循环；其他值必须大于 0</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="count"/> 不是 -1 且小于 1 时抛出。</exception>
    public Tween SetLoops(int count)
    {
        if (count != -1 && count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Loop count must be -1 or greater than 0.");

        _loopCount = count;
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
    /// 终止 Tween。从任何状态调用均有效，进入 <see cref="TweenState.Finished"/>。
    /// <see cref="TweenManager"/> 会在下一帧自动回收。
    /// </summary>
    public void Kill()
    {
        if (State == TweenState.Finished)
            return;
        State = TweenState.Finished;
        OnFinished?.Invoke();
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

        // 全部 Step 已完成
        if (_currentStepIndex >= _steps.Count)
        {
            AdvanceLoop();
            return;
        }

        var step = _steps[_currentStepIndex];
        var allDone = true;

        foreach (var entry in step.Entries)
        {
            if (!entry.IsCompleted)
                entry.Tick(deltaSeconds);
            if (!entry.IsCompleted)
                allDone = false;
        }

        if (allDone)
        {
            _currentStepIndex++;

            if (_currentStepIndex >= _steps.Count)
                AdvanceLoop();
        }
    }

    #endregion

    #region 内部方法

    /// <summary> 推进循环 </summary>
    private void AdvanceLoop()
    {
        _completedLoops++;

        if (_loopCount == -1)
        {
            ResetSteps(); return;
        }

        if (_completedLoops < _loopCount) ResetSteps();
        else Kill();
    }

    private void ResetSteps()
    {
        _currentStepIndex = 0;
        foreach (var entry in _steps.SelectMany(s => s.Entries))
            entry.Reset();
    }

    #endregion
}
