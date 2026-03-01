namespace SilkyUIFramework.Components;

/// <summary>
/// 矩形样式绘制器，封装背景、边框和阴影的渲染参数与绘制入口。
/// </summary>
public class RectangleRender
{
    /// <summary>
    /// 边框宽度。
    /// </summary>
    public float Border { get; set; }

    /// <summary>
    /// 边框颜色。
    /// </summary>
    public Color BorderColor { get; set; } = Color.Transparent;

    /// <summary>
    /// 背景颜色。
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;

    /// <summary>
    /// 四个角的圆角半径（左上、右上、右下、左下）。
    /// </summary>
    public Vector4 BorderRadius { get; set; } = Vector4.Zero;

    /// <summary>
    /// 绘制背景与边框。
    /// </summary>
    /// <param name="position">矩形左上角坐标。</param>
    /// <param name="size">矩形尺寸。</param>
    /// <param name="matrix">用于顶点变换的矩阵。</param>
    public void Draw(Vector2 position, Vector2 size, ref Matrix matrix)
    {
        if (size.X <= 0 || size.Y <= 0) return;

        if (Border > 0f)
        {
            if (BorderColor == Color.Transparent)
            {
                if (BackgroundColor != Color.Transparent)
                    SDFRectangle.DrawWithoutBorder(
                        position + new Vector2(Border), size - new Vector2(Border * 2f),
                        BorderRadius - new Vector4(Border), BackgroundColor, matrix
                    );
            }
            else
            {
                SDFRectangle.DrawWithBorder(position, size, BorderRadius, BackgroundColor, Border, BorderColor, matrix);
            }
        }
        else if (BackgroundColor != Color.Transparent)
        {
            SDFRectangle.DrawWithoutBorder(position, size, BorderRadius, BackgroundColor, matrix);
        }
    }

    /// <summary>
    /// 从另一个渲染器复制样式参数。
    /// </summary>
    public void CopyStyle(RectangleRender rectangleRender)
    {
        BorderRadius = rectangleRender.BorderRadius;
        Border = rectangleRender.Border;
        BackgroundColor = rectangleRender.BackgroundColor;
        BorderColor = rectangleRender.BorderColor;

        ShadowSize = rectangleRender.ShadowSize;
        ShadowBlurSize = rectangleRender.ShadowBlurSize;
        ShadowColor = rectangleRender.ShadowColor;
    }

    /// <summary>
    /// 阴影向外扩展距离。
    /// </summary>
    public float ShadowSize { get; set; } = 10f;

    /// <summary>
    /// 阴影模糊半径。
    /// </summary>
    public float ShadowBlurSize { get; set; } = 10f;

    /// <summary>
    /// 阴影颜色。
    /// </summary>
    public Color ShadowColor { get; set; } = Color.Transparent;

    /// <summary>
    /// 绘制阴影。
    /// </summary>
    /// <remarks>
    /// 阴影绘制时会按 <see cref="ShadowSize"/> 扩展矩形区域，并同步扩大圆角半径。
    /// </remarks>
    public void DrawShadow(Vector2 position, Vector2 size, ref Matrix matrix)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        if (ShadowColor == Color.Transparent) return;

        position -= new Vector2(ShadowSize);
        size += new Vector2(ShadowSize * 2);
        SDFRectangle.DrawShadow(position, size, BorderRadius + new Vector4(ShadowSize), ShadowColor, ShadowBlurSize, matrix);
    }

    /// <summary>
    /// 创建当前样式的浅拷贝实例。
    /// </summary>
    public RectangleRender Clone() => MemberwiseClone() as RectangleRender;

    public override string ToString() =>
        $"RectangleRender(Border={Border}, BackgroundColor={BackgroundColor}, BorderColor={BorderColor}, BorderRadius={BorderRadius}, ShadowSize={ShadowSize}, ShadowBlurSize={ShadowBlurSize}, ShadowColor={ShadowColor})";
}
