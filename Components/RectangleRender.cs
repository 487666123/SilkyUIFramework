namespace SilkyUIFramework.Components;

public class RectangleRender
{
    public float Border { get; set; }
    public Color BorderColor { get; set; } = Color.Transparent;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public Vector4 BorderRadius { get; set; } = Vector4.Zero;

    public void Draw(Vector2 position, Vector2 size, bool noBorder, ref Matrix matrix)
    {
        if (Border > 0f)
        {
            if (BorderColor == Color.Transparent || noBorder)
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

    public void DrawOnlyBorder(Vector2 position, Vector2 size, Matrix matrix) =>
        SDFRectangle.DrawWithBorder(position, size, BorderRadius, Color.Transparent, Border, BorderColor, matrix);

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

    public float ShadowSize { get; set; } = 10f;
    public float ShadowBlurSize { get; set; } = 10f;
    public Color ShadowColor { get; set; } = Color.Transparent;

    public void DrawShadow(Vector2 position, Vector2 size, ref Matrix matrix)
    {
        if (ShadowColor == Color.Transparent) return;

        position -= new Vector2(ShadowSize);
        size += new Vector2(ShadowSize * 2);
        SDFRectangle.DrawShadow(position, size, BorderRadius + new Vector4(ShadowSize), ShadowColor, ShadowBlurSize, matrix);
    }

    public RectangleRender Clone() => MemberwiseClone() as RectangleRender;

    public override string ToString()
    {
        return $"border: {Border}, bgColor: {BackgroundColor}, borderColor: {BorderColor}, cornerRadius: {BorderRadius}";
    }
}