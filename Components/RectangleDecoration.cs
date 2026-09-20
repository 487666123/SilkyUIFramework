using System;
using SilkyUIFramework.Graphics2D.Rectangles;

namespace SilkyUIFramework.Components;

/// <summary>
/// 保存矩形的背景、边框、圆角和阴影样式，并通过绘制器绘制装饰。
/// 透明边框仍占据宽度，背景位于边框内侧。
/// 绘制时将负的宽度、阴影扩展和圆角按零处理，不修改保存的样式。
/// </summary>
public class RectangleDecoration
{
    // 图形线程首次实际绘制时创建，所有装饰共用同一个绘制器。
    private static RectangleRenderer Renderer =>
        field ??= new RectangleRenderer(
            Main.graphics.GraphicsDevice,
            ModAsset.SDFRectangle.Value,
            Main.spriteBatch.spriteEffectPass);

    /// <summary>
    /// 边框宽度。
    /// </summary>
    public float BorderWidth { get; set; }

    /// <summary>
    /// 边框颜色。
    /// </summary>
    public Color BorderColor { get; set; } = Color.Transparent;

    /// <summary>
    /// 背景颜色。
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;

    /// <summary>
    /// 四个角的圆角半径（左上、右上、左下、右下）。
    /// </summary>
    public Vector4 CornerRadii { get; set; } = Vector4.Zero;

    /// <summary>
    /// 阴影向外扩展距离。
    /// </summary>
    public float ShadowSpread { get; set; } = 10f;

    /// <summary>
    /// 阴影向内部淡出的过渡宽度。
    /// </summary>
    public float ShadowBlurWidth { get; set; } = 10f;

    /// <summary>
    /// 阴影颜色。
    /// </summary>
    public Color ShadowColor { get; set; } = Color.Transparent;

    /// <summary>
    /// 绘制背景与边框，不包含阴影。
    /// </summary>
    /// <param name="position">矩形左上角坐标。</param>
    /// <param name="size">矩形尺寸。</param>
    /// <param name="matrix">用于顶点变换的矩阵。</param>
    public void DrawSurface(Vector2 position, Vector2 size, Matrix matrix)
    {
        if (size.X <= 0f || size.Y <= 0f) return;

        var borderWidth = MathF.Max(0f, BorderWidth);
        var cornerRadii = Vector4.Max(CornerRadii, Vector4.Zero);

        if (borderWidth > 0f && BorderColor != Color.Transparent)
        {
            Renderer.DrawBordered(position, size, cornerRadii, BackgroundColor, borderWidth, BorderColor, matrix);
            return;
        }

        if (BackgroundColor == Color.Transparent) return;

        // 透明边框仍占据宽度，因此背景需要向内收缩。
        var innerPosition = position + new Vector2(borderWidth);
        var innerSize = size - new Vector2(borderWidth * 2f);

        if (innerSize.X <= 0f || innerSize.Y <= 0f) return;

        var innerRadii = Vector4.Max(cornerRadii - new Vector4(borderWidth), Vector4.Zero);

        Renderer.DrawFill(innerPosition, innerSize, innerRadii, BackgroundColor, matrix);
    }

    /// <summary>
    /// 根据主体区域推导阴影范围，并绘制阴影。
    /// </summary>
    /// <remarks>
    /// 阴影绘制时会按 <see cref="ShadowSpread"/> 扩展矩形区域，并同步扩大圆角半径。
    /// </remarks>
    /// <param name="position">主体矩形左上角坐标。</param>
    /// <param name="size">主体矩形尺寸。</param>
    /// <param name="matrix">用于顶点变换的矩阵。</param>
    public void DrawShadow(Vector2 position, Vector2 size, Matrix matrix)
    {
        if (size.X <= 0f || size.Y <= 0f) return;

        if (ShadowColor == Color.Transparent) return;

        var spread = MathF.Max(0f, ShadowSpread);
        var blurWidth = MathF.Max(0f, ShadowBlurWidth);
        var cornerRadii = Vector4.Max(CornerRadii, Vector4.Zero);

        var shadowPosition = position - new Vector2(spread);
        var shadowSize = size + new Vector2(spread * 2f);
        var shadowRadii = cornerRadii + new Vector4(spread);

        Renderer.DrawShadow(shadowPosition, shadowSize, shadowRadii, ShadowColor, blurWidth, matrix);
    }
}
