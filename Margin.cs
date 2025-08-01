namespace SilkyUIFramework;

public readonly struct Margin(float left, float top, float right, float bottom) : IEquatable<Margin>
{
    public static Margin Zero { get; } = new(0f, 0f, 0f, 0f);

    public Margin(float uniform) : this(uniform, uniform, uniform, uniform) { }

    public Margin(float hUniform, float vUniform) : this(hUniform, vUniform, hUniform, vUniform) { }

    public float Left { get; } = left;
    public float Top { get; } = top;
    public float Right { get; } = right;
    public float Bottom { get; } = bottom;

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public static implicit operator Margin(float margin) => new Margin(margin);

    public Margin With(float? left = null, float? top = null, float? right = null, float? bottom = null) =>
        new(left ?? Left, top ?? Top, right ?? Right, bottom ?? Bottom);

    public static Vector2 operator +(Vector2 position, Margin margin) =>
        new(position.X + margin.Left, position.Y + margin.Top);

    public static Vector2 operator -(Vector2 position, Margin margin) =>
        new(position.X - margin.Left, position.Y - margin.Top);

    public static bool operator ==(Margin left, Margin right) => left.Equals(right);

    public static bool operator !=(Margin left, Margin right) => left.Equals(right);

    public override bool Equals(object obj) => obj is Margin margin && Equals(margin);

    public bool Equals(Margin other) =>
        Left.Equals(other.Left) && Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public override string ToString()
    {
        return $"{nameof(Left)}: {Left}, {nameof(Top)}: {Top}, {nameof(Right)}: {Right}, {nameof(Bottom)}: {Bottom}";
    }
}