using SilkyUIFramework.Common.Tweening;

namespace SilkyUIFramework.Extensions;

/// <summary>
/// 面向 SilkyUI 视图的 Tween 便捷扩展。
/// 这些方法只负责向既有 <see cref="Tween"/> 添加条目，不创建新的 Tween。
/// </summary>
public static class UIViewTweenExtensions
{
    extension(Tween tween)
    {
        /// <summary>
        /// 补间 <see cref="BaseBody"/> 的离屏合成透明度。
        /// </summary>
        public TweenEntry FadeTo(BaseBody body, float opacity, float duration)
        {
            return tween.TweenProperty(
                body,
                static (target, value) => target.Opacity = value,
                static target => target.Opacity,
                opacity,
                duration,
                MathHelper.Lerp);
        }

        /// <summary>
        /// 补间 <see cref="UIView"/> 的背景色。
        /// </summary>
        public TweenEntry BgColorTo(UIView view, Color color, float duration)
        {
            return tween.TweenProperty(
                view,
                static (target, value) => target.BackgroundColor = value,
                static target => target.BackgroundColor,
                color,
                duration,
                Color.Lerp);
        }

        /// <summary>
        /// 补间 <see cref="UIView"/> 的边框色。
        /// </summary>
        public TweenEntry BorderColorTo(UIView view, Color color, float duration)
        {
            return tween.TweenProperty(
                view,
                static (target, value) => target.BorderColor = value,
                static target => target.BorderColor,
                color,
                duration,
                Color.Lerp);
        }
    }
}
