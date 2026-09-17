using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilkyUIFramework;

public readonly struct Margin(float left, float top, float right, float bottom) : IEquatable<Margin>, IParsable<Margin>, IFormattable
{
    public static Margin Zero { get; } = new(0f, 0f, 0f, 0f);

    public Margin(float uniform) : this(uniform, uniform, uniform, uniform) { }

    public Margin(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }

    public void Deconstruct(out float left, out float top, out float right, out float bottom)
    {
        left = Left; top = Top; right = Right; bottom = Bottom;
    }

    public float Left { get; } = left;
    public float Top { get; } = top;
    public float Right { get; } = right;
    public float Bottom { get; } = bottom;

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public static implicit operator Margin(float margin) => new(margin);

    public Margin With(float? left = null, float? top = null, float? right = null, float? bottom = null) =>
        new(left ?? Left, top ?? Top, right ?? Right, bottom ?? Bottom);

    /// <summary>按左、上、右、下分别进行线性插值。</summary>
    public static Margin Lerp(Margin a, Margin b, float t) =>
        new(MathHelper.Lerp(a.Left, b.Left, t),
            MathHelper.Lerp(a.Top, b.Top, t),
            MathHelper.Lerp(a.Right, b.Right, t),
            MathHelper.Lerp(a.Bottom, b.Bottom, t));

    public static Vector2 operator +(Vector2 position, Margin margin) =>
        new(position.X + margin.Left, position.Y + margin.Top);

    public static Vector2 operator -(Vector2 position, Margin margin) =>
        new(position.X - margin.Left, position.Y - margin.Top);

    public static bool operator ==(Margin left, Margin right) => left.Equals(right);

    public static bool operator !=(Margin left, Margin right) => !left.Equals(right);

    public override bool Equals(object obj) => obj is Margin margin && Equals(margin);

    public bool Equals(Margin other) =>
        Left.Equals(other.Left) && Top.Equals(other.Top) && Right.Equals(other.Right) && Bottom.Equals(other.Bottom);

    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

    public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

    public string ToString(IFormatProvider provider) => ToString(null, provider);

    /// <summary> 始终按左、上、右、下输出四个数字；provider 为 null 时使用当前文化。 </summary>
    public string ToString(string format, IFormatProvider formatProvider) =>
        $"{Left.ToString(format, formatProvider)} {Top.ToString(format, formatProvider)} " +
        $"{Right.ToString(format, formatProvider)} {Bottom.ToString(format, formatProvider)}";

    public static Margin Parse(string s, IFormatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(s);

        return TryParse(s, provider, out var result) ? result :
            throw new FormatException($"Cannot parse '{s}' as Margin.");
    }

    /// <summary>
    /// 解析 1、2 或 4 个空白分隔的裸数字：单值用于四边，两值为水平、垂直，四值为左、上、右、下。
    /// 不接受三个值或单位后缀。数字允许正负号、小数和科学计数法，但必须有限。
    /// provider 为 null 时使用当前文化。
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out Margin result)
    {
        result = default;
        return s is not null && TryParseCore(s.AsSpan(), provider, out result);
    }

    private static bool TryParseCore(ReadOnlySpan<char> text, IFormatProvider provider, out Margin result)
    {
        result = default;

        var remaining = text.Trim();
        if (remaining.IsEmpty) return false;

        Span<float> values = stackalloc float[4];
        var count = 0;
        while (!remaining.IsEmpty)
        {
            if (count == values.Length) return false;

            var length = 0;
            while (length < remaining.Length && !char.IsWhiteSpace(remaining[length]))
            {
                length++;
            }

            var token = remaining[..length];
            remaining = remaining[length..].TrimStart();

            if (!float.TryParse(token, NumberStyles.Float, provider, out var value) ||
                !float.IsFinite(value)) return false;

            values[count++] = value;
        }

        // 只有数量和全部数值都有效时才构造结果。
        switch (count)
        {
            case 1:
                result = new Margin(values[0]);
                return true;
            case 2:
                result = new Margin(values[0], values[1]);
                return true;
            case 4:
                result = new Margin(values[0], values[1], values[2], values[3]);
                return true;
            default:
                return false;
        }
    }
}