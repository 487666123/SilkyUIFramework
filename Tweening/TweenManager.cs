using System;
using System.Collections.Generic;

namespace SilkyUIFramework.Tweening;

/// <summary>
/// Tween 全局管理器。负责创建、更新和回收 Tween。
/// <example>
/// 基本用法：
/// <code>
/// var manager = new TweenManager();
/// var tween = manager.CreateTween();
/// tween.TweenProperty&lt;float&gt;(v => obj.X = v, () => obj.X, 100f, 0.5f, Lerp)
///      .SetEase(EaseType.Out);
/// // 每帧调用:
/// manager.Update(gameTime.TotalGameTime);
/// </code>
/// </example>
/// </summary>
public class TweenManager
{
    public static TweenManager Instance { get; } = new();

    private readonly List<Tween> _active = [];
    private readonly List<Tween> _pending = [];
    private TimeSpan? _lastTime;

    /// <summary>当前活跃的 Tween 数量，不包含等待下一次 Update 合并的 pending Tween。</summary>
    public int ActiveCount => _active.Count;

    /// <summary>
    /// 创建 Tween 并自动注册到此管理器。
    /// </summary>
    public Tween CreateTween()
    {
        var tween = new Tween();
        tween.Play();
        Register(tween);
        return tween;
    }

    /// <summary>
    /// 注册已有的 Tween。注册后由管理器驱动更新和回收。
    /// </summary>
    public void Register(Tween tween)
    {
        if (tween == null) return;
        _pending.Add(tween);
    }

    /// <summary>终止并回收所有 Tween。</summary>
    public void Clear()
    {
        foreach (var tween in _active)
            tween.Kill();
        foreach (var tween in _pending)
            tween.Kill();
        _active.Clear();
        _pending.Clear();
    }

    /// <summary>
    /// 驱动所有活跃 Tween。根据传入时间与上次调用时间计算 delta。
    /// </summary>
    public void Update(TimeSpan time)
    {
        if (_lastTime is null)
        {
            _lastTime = time;
            return;
        }
        float delta = (float)(time - _lastTime.Value).TotalSeconds;
        _lastTime = time;

        // 合并待注册的 tween
        if (_pending.Count > 0)
        {
            _active.AddRange(_pending);
            _pending.Clear();
        }

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var tween = _active[i];

            // 检查有效性
            if (tween.ValidityCheck != null && !tween.ValidityCheck())
            {
                tween.Kill();
                _active.RemoveAt(i);
                continue;
            }

            // 已结束 → 回收
            if (tween.IsFinished)
            {
                _active.RemoveAt(i);
                continue;
            }

            tween.Update(delta);

            // 更新后再次检查
            if (tween.IsFinished)
            {
                _active.RemoveAt(i);
            }
        }
    }
}
