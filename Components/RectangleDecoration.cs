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
            ModAsset.RectangleEffect.Value,
            Main.spriteBatch.spriteEffectPass);

    private float _borderWidthLeft;
    private float _borderWidthTop;
    private float _borderWidthRight;
    private float _borderWidthBottom;

    /// <summary>
    /// 边宽实际改变后通知订阅者。统一设置四边时，在全部更新完成后只通知一次。
    /// </summary>
    public event Action BorderWidthsChanged;

    /// <summary>
    /// 统一设置四边宽度；可随后通过各边属性单独覆盖。
    /// </summary>
    public float BorderWidth
    {
        set
        {
            if (_borderWidthLeft == value && _borderWidthTop == value &&
                _borderWidthRight == value && _borderWidthBottom == value) return;

            _borderWidthLeft = value;
            _borderWidthTop = value;
            _borderWidthRight = value;
            _borderWidthBottom = value;
            BorderWidthsChanged?.Invoke();
        }
    }

    public float BorderWidthLeft
    {
        get => _borderWidthLeft;
        set
        {
            if (_borderWidthLeft == value) return;
            _borderWidthLeft = value;
            BorderWidthsChanged?.Invoke();
        }
    }

    public float BorderWidthTop
    {
        get => _borderWidthTop;
        set
        {
            if (_borderWidthTop == value) return;
            _borderWidthTop = value;
            BorderWidthsChanged?.Invoke();
        }
    }

    public float BorderWidthRight
    {
        get => _borderWidthRight;
        set
        {
            if (_borderWidthRight == value) return;
            _borderWidthRight = value;
            BorderWidthsChanged?.Invoke();
        }
    }

    public float BorderWidthBottom
    {
        get => _borderWidthBottom;
        set
        {
            if (_borderWidthBottom == value) return;
            _borderWidthBottom = value;
            BorderWidthsChanged?.Invoke();
        }
    }

    public Color BorderColorLeft { get; set; }

    public Color BorderColorTop { get; set; }

    public Color BorderColorRight { get; set; }

    public Color BorderColorBottom { get; set; }

    /// <summary>
    /// 统一设置四边颜色；可随后通过各边属性单独覆盖。
    /// </summary>
    public Color BorderColor
    {
        set
        {
            BorderColorLeft = value;
            BorderColorTop = value;
            BorderColorRight = value;
            BorderColorBottom = value;
        }
    }

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
    /// 绘制背景与边框，不包含阴影，根据有效边的宽度、颜色及几何条件选择绘制路径。
    /// </summary>
    /// <param name="position">矩形左上角坐标。</param>
    /// <param name="size">矩形尺寸。</param>
    /// <param name="matrix">用于顶点变换的矩阵。</param>
    public void DrawSurface(Vector2 position, Vector2 size, Matrix matrix)
    {
        if (size.X <= 0f || size.Y <= 0f) return;

        // 顺序为左、上、右、下，只规范本次绘制的数据，不回写样式属性。
        var widths = Vector4.Max(new Vector4(
            BorderWidthLeft, BorderWidthTop, BorderWidthRight, BorderWidthBottom), Vector4.Zero);

        if (widths == Vector4.Zero)
        {
            if (BackgroundColor != Color.Transparent)
                Renderer.DrawFill(position, size, Vector4.Max(CornerRadii, Vector4.Zero), BackgroundColor, matrix);
            return;
        }

        var leftColor = BorderColorLeft;
        var topColor = BorderColorTop;
        var rightColor = BorderColorRight;
        var bottomColor = BorderColorBottom;
        var hasVisibleBorder =
            (widths.X > 0f && leftColor != Color.Transparent) ||
            (widths.Y > 0f && topColor != Color.Transparent) ||
            (widths.Z > 0f && rightColor != Color.Transparent) ||
            (widths.W > 0f && bottomColor != Color.Transparent);

        if (!hasVisibleBorder && BackgroundColor == Color.Transparent) return;

        var cornerRadii = Vector4.Max(CornerRadii, Vector4.Zero);
        // 零宽边的颜色没有贡献，不应因此进入四色路径。
        var sharedColor = widths.X > 0f ? leftColor
            : widths.Y > 0f ? topColor
            : widths.Z > 0f ? rightColor
            : bottomColor;
        var sameColor =
            (widths.X == 0f || leftColor == sharedColor) &&
            (widths.Y == 0f || topColor == sharedColor) &&
            (widths.Z == 0f || rightColor == sharedColor) &&
            (widths.W == 0f || bottomColor == sharedColor);

        if (!sameColor)
        {
            Renderer.DrawBordered(position, size, cornerRadii, BackgroundColor, widths,
                leftColor, topColor, rightColor, bottomColor, matrix);
            return;
        }

        var innerSize = size - new Vector2(widths.X + widths.Z, widths.Y + widths.W);
        if (innerSize.X <= 0f || innerSize.Y <= 0f)
        {
            if (sharedColor != Color.Transparent)
                Renderer.DrawFill(position, size, cornerRadii, sharedColor, matrix);
            return;
        }

        var sameWidth = widths.X == widths.Y && widths.X == widths.Z && widths.X == widths.W;
        var transparentBorder = sharedColor == Color.Transparent;
        if (sameWidth && CanUseSimpleUniformBorder(
            widths.X, cornerRadii, size, innerSize, matrix, transparentBorder))
        {
            if (transparentBorder)
            {
                // 只有内圆角仍可用普通圆角表示时，才直接绘制收缩后的背景。
                Renderer.DrawFill(position + new Vector2(widths.X), innerSize,
                    cornerRadii - new Vector4(widths.X), BackgroundColor, matrix);
            }
            else
            {
                Renderer.DrawBordered(position, size, cornerRadii,
                    BackgroundColor, widths.X, sharedColor, matrix);
            }
            return;
        }

        // 不等宽的透明边框仍占空间；内圆角可能是椭圆，不能简单收缩后 DrawFill。
        Renderer.DrawBordered(position, size, cornerRadii, BackgroundColor, widths, sharedColor, matrix);
    }

    /// <summary>
    /// 保守判断统一描边能否使用简单路径，避免切换 pass 时改变内轮廓或抗锯齿混色。
    /// 与 RectangleRenderer 的正等比缩放、padding 和抗锯齿范围约定一致。
    /// </summary>
    private static bool CanUseSimpleUniformBorder(float width, Vector4 cornerRadii,
        Vector2 size, Vector2 innerSize, Matrix matrix, bool transparentBorder)
    {
        var edgePadding = 1f / matrix.M11;
        var antialiasWidth = 1.414213562373f / matrix.M11;

        // 新路径对很窄的内部区域有限制覆盖率的处理，简单路径没有。
        if (innerSize.X < antialiasWidth || innerSize.Y < antialiasWidth)
            return false;

        var minRadius = MathF.Min(MathF.Min(cornerRadii.X, cornerRadii.Y), MathF.Min(cornerRadii.Z, cornerRadii.W));
        var maxRadius = MathF.Max(MathF.Max(cornerRadii.X, cornerRadii.Y), MathF.Max(cornerRadii.Z, cornerRadii.W));

        // 保证圆弧位于各自象限内，统一收缩时不需要重新缩放内圆角。
        if (maxRadius + edgePadding > MathF.Min(size.X, size.Y) * 0.5f)
            return false;

        if (transparentBorder)
            return minRadius >= width;

        // 旧描边按覆盖率乘积混色。内外 AA 不重叠、内圆角不出现负半径时才等价。
        return width >= antialiasWidth && minRadius + edgePadding >= width;
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
