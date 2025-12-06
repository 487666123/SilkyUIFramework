namespace SilkyUIFramework;

public struct Bounds(float x, float y, float width, float height)
{
    public float X { get; set; } = x;

    public float Y { get; set; } = y;

    public float Width { get; set; } = width;

    public float Height { get; set; } = height;

    public Bounds(Vector2 position, Size size) : this(position.X, position.Y, size.Width, size.Height) { }

    public static Bounds Zero { get; } = new(0, 0, 0, 0);

    public Vector2 Position
    {
        readonly get => new(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public Size Size
    {
        readonly get => new(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    public readonly float Right => X + Width;
    public readonly float Bottom => Y + Height;
    public readonly Vector2 Center => new(X + Width / 2f, Y + Height / 2f);
    public readonly Vector2 TopLeft => new(X, Y);
    public readonly Vector2 TopRight => new(X + Width, Y);
    public readonly Vector2 BottomLeft => new(X, Y + Height);
    public readonly Vector2 BottomRight => new(X + Width, Y + Height);

    public static implicit operator Bounds(Rectangle rectangle)
    {
        return new Bounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    public static implicit operator Rectangle(Bounds bounds)
    {
        return new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height);
    }

    /// <summary>
    /// 判断点是否在内
    /// </summary>
    public readonly bool Contains(Vector2 point)
    {
        return point.X >= X &&
               point.X <= X + Width &&
               point.Y >= Y &&
               point.Y <= Y + Height;
    }

    /// <summary>
    /// 判断两个矩形是否有重叠部分
    /// </summary>
    public readonly bool Intersects(Bounds other)
    {
        return X < other.X + other.Width &&
               X + Width > other.X &&
               Y < other.Y + other.Height &&
               Y + Height > other.Y;
    }

    public readonly override string ToString()
    {
        return $"Bounds: {X}, {Y}, {Width}, {Height}";
    }
}