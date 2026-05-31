using System;

namespace SilkyUIFramework.Tween;

/// <summary>
/// 回调条目。在延迟到期后触发一次回调即完成。
/// Duration 固定为 0，仅 Delay 影响触发时机。
/// </summary>
public class TweenCallback : TweenEntry
{
    /// <summary>触发时执行的回调委托</summary>
    private readonly Action _callback;

    /// <summary>
    /// 创建回调条目。
    /// </summary>
    /// <param name="callback">触发时执行的回调</param>
    public TweenCallback(Action callback)
    {
        _callback = callback;
        Duration = 0f;
    }

    internal override void Tick(float delta)
    {
        if (IsCompleted) return;

        Elapsed += delta;
        if (Elapsed < Delay) return;

        _callback();
        IsCompleted = true;
    }
}
