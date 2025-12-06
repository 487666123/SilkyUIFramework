using ReLogic.Content;

namespace SilkyUIFramework.Components;

public enum TileMode
{
    Adaptive,
}

public sealed class NinePatch
{
    public Asset<Texture2D> Texture2D { get; set; }

    private int _width;
    private int _height;

    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }

    public Rectangle TopLeft => new(0, 0, Left, Top);
    public Rectangle TopRight => new(_width - Right, 0, Right, Top);
    public Rectangle BottomLeft => new(0, _height - Bottom, Left, Bottom);
    public Rectangle BottomRight => new(_width - Right, _height - Bottom, Right, Bottom);

    public Rectangle LeftBorder => new(0, Top, Left, _height - Bottom);
    public Rectangle TopBorder => new(Left, 0, _width - Left - Right, Top);
    public Rectangle RightBorder => new(_width - Right, Top, Right, _height - Top - Bottom);
    public Rectangle BottomBorder => new(Left, _height - Bottom, _width - Left - Right, Bottom);

    public Rectangle MiddleArea => new(Left, Top, _width - Right, _height - Bottom);

    public void Draw(SpriteBatch batch, Vector2 position, Vector2 size, Color color, Vector2 origin, Vector2 scale)
    {
        var texture = Texture2D.Value;
        if (texture == null) return;
        _width = texture.Width; _height = texture.Height;

        if (size.X < Left + Right) size.X = Left + Right;
        if (size.Y < Top + Bottom) size.Y = Top + Bottom;

        // 角
        DrawCorner(batch, position, size, color, origin, scale);
        // 边
        // DrawBorder(batch, position, color, origin, scale);
        // 中间
        DrawMiddleArea(batch, position, color, origin, scale);
    }

    private void DrawCorner(SpriteBatch batch, Vector2 position, Vector2 size, Color color, Vector2 origin, Vector2 scale)
    {
        var texture = Texture2D.Value;
        batch.Draw(texture, position, TopLeft, color, 0f, origin, scale, 0, 0f);
        batch.Draw(texture, position.AddX(size.X - Right), TopRight, color, 0f, origin, scale, 0, 0f);
        batch.Draw(texture, position.AddY(size.Y - Bottom), BottomLeft, color, 0f, origin, scale, 0, 0f);
        batch.Draw(texture, position + size - new Vector2(Right, Bottom), BottomRight, color, 0f, origin, scale, 0, 0f);
    }

    private void DrawBorder(SpriteBatch batch, Vector2 position, Color color, Vector2 origin, Vector2 scale)
    {
        var texture = Texture2D.Value;
        batch.Draw(texture, position, LeftBorder, color, 0f, origin, scale, 0, 0f);
        batch.Draw(texture, position, TopBorder, color, 0f, origin, scale, 0, 0f);
        batch.Draw(texture, position, RightBorder, color, 0f, origin, scale, 0, 0f);
        batch.Draw(texture, position, BottomBorder, color, 0f, origin, scale, 0, 0f);
    }

    private void DrawMiddleArea(SpriteBatch batch, Vector2 position, Color color, Vector2 origin, Vector2 scale)
    {
        batch.Draw(Texture2D.Value, position, MiddleArea, color, 0f, origin, scale, 0, 0f);
    }
}
