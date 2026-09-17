using SilkyUIFramework.Common.Tweening;

namespace SilkyUIFramework.StyleSystem;

/// <summary>属性过渡配置。没有可播放的配置时直接赋值。</summary>
public sealed class StyleTransition
{
    /// <summary>持续时间，单位为秒；负数按零处理。</summary>
    public float Duration { get; set; } = 0.2f;

    public EaseType Ease { get; set; } = EaseType.Out;
    public TransitionType Trans { get; set; } = TransitionType.Expo;

    /// <summary>开始前的延迟，单位为秒；负数按零处理。</summary>
    public float Delay { get; set; } = 0f;

    /// <summary>允许仅延迟、不插值；时长与延迟均不为正时直接赋值。</summary>
    public bool CanPlay() =>
        float.IsFinite(Duration) && float.IsFinite(Delay) &&
        (Duration > 0f || Delay > 0f) && Enum.IsDefined(Ease) && Enum.IsDefined(Trans);
}
