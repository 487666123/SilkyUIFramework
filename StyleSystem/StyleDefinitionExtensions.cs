namespace SilkyUIFramework.StyleSystem;

/// <summary>常用样式的便捷写法；最终仍通过 Set 保存属性路径和值。</summary>
public static class StyleDefinitionExtensions
{
    public static StyleDefinition Background(this StyleDefinition style, Color color) =>
        style.Set("BackgroundColor", color);

    public static StyleDefinition BorderColor(this StyleDefinition style, Color color) =>
        style.Set("BorderColor", color);

    public static StyleDefinition BorderWidth(this StyleDefinition style, float width) =>
        style.Set("Border", width);

    public static StyleDefinition Border(this StyleDefinition style, Color color, float width) =>
        style.BorderColor(color).BorderWidth(width);

    public static StyleDefinition Rounded(this StyleDefinition style, float radius) =>
        style.Set("BorderRadius", new Vector4(radius));

    /// <summary>四角顺序：左上、右上、右下、左下。</summary>
    public static StyleDefinition Rounded(this StyleDefinition style, float topLeft, float topRight, float bottomRight, float bottomLeft) =>
        style.Set("BorderRadius", new Vector4(topLeft, topRight, bottomRight, bottomLeft));

    public static StyleDefinition Padding(this StyleDefinition style, float padding) =>
        style.Set("Padding", new Margin(padding));

    public static StyleDefinition Padding(this StyleDefinition style, float left, float top, float right, float bottom) =>
        style.Set("Padding", new Margin(left, top, right, bottom));

    public static StyleDefinition Margin(this StyleDefinition style, float margin) =>
        style.Set("Margin", new Margin(margin));

    public static StyleDefinition Margin(this StyleDefinition style, float left, float top, float right, float bottom) =>
        style.Set("Margin", new Margin(left, top, right, bottom));

    public static StyleDefinition Opacity(this StyleDefinition style, float opacity) =>
        style.Set("Opacity", MathHelper.Clamp(opacity, 0f, 1f));

    public static StyleDefinition TextColor(this StyleDefinition style, Color color) =>
        style.Set("TextColor", color);

    public static StyleDefinition TextScale(this StyleDefinition style, float scale) =>
        style.Set("TextScale", scale);

    public static StyleDefinition Width(this StyleDefinition style, float width) =>
        style.Set("Width", new Dimension(width));

    public static StyleDefinition Height(this StyleDefinition style, float height) =>
        style.Set("Height", new Dimension(height));

    public static StyleDefinition Size(this StyleDefinition style, float width, float height) =>
        style.Width(width).Height(height);

    public static StyleDefinition Nested(this StyleDefinition style, string path, object value) =>
        style.Set(path, value);
}
