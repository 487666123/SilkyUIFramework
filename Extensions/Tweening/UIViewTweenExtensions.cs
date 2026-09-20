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
        /// 将四条边分别从各自当前颜色补间到同一个目标颜色。
        /// 一个条目统一控制四边，在延迟结束时捕获起始颜色。
        /// </summary>
        public TweenEntry BorderColorTo(UIView view, Color color, float duration)
        {
            return tween.TweenProperty<UIView, (Color Left, Color Top, Color Right, Color Bottom)>(
                view,
                static (target, value) =>
                {
                    var decoration = target.RectangleDecoration;
                    decoration.BorderColorLeft = value.Left;
                    decoration.BorderColorTop = value.Top;
                    decoration.BorderColorRight = value.Right;
                    decoration.BorderColorBottom = value.Bottom;
                },
                static target =>
                {
                    var decoration = target.RectangleDecoration;
                    return (decoration.BorderColorLeft, decoration.BorderColorTop,
                        decoration.BorderColorRight, decoration.BorderColorBottom);
                },
                (color, color, color, color),
                duration,
                static (from, to, amount) => (
                    Color.Lerp(from.Left, to.Left, amount),
                    Color.Lerp(from.Top, to.Top, amount),
                    Color.Lerp(from.Right, to.Right, amount),
                    Color.Lerp(from.Bottom, to.Bottom, amount)));
        }
    }
}
