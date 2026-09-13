using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilkyUIFramework;

public readonly struct Size(float width, float height) : IEquatable<Size>, IParsable<Size>, IFormattable
{
    public static readonly Size Zero = new(0, 0);

    public Size(float size) : this(size, size) { }

    public float Width { get; } = width;
    public float Height { get; } = height;

    public Size With(float? width = null, float? height = null)
    {
        return new Size(width ?? Width, height ?? Height);
    }

    public static implicit operator Size(float gap)
    {
        return new Size(gap);
    }

    public static implicit operator Size(Vector2 vector2)
    {
        return new Size(vector2.X, vector2.Y);
    }

    public static implicit operator Vector2(Size size)
    {
        return new Vector2(size.Width, size.Height);
    }

    public static Size operator +(Size size1, Size size2)
    {
        return new Size(size1.Width + size2.Width, size1.Height + size2.Height);
    }

    public static Size operator -(Size size1, Size size2)
    {
        return new Size(size1.Width - size2.Width, size1.Height - size2.Height);
    }

    public static Size operator +(Vector2 vector2, Size size)
    {
        return new Size(vector2.X + size.Width, vector2.Y + size.Height);
    }

    public static Size operator -(Vector2 vector2, Size size)
    {
        return new Size(vector2.X - size.Width, vector2.Y - size.Height);
    }

    public static Size operator +(Size size, Vector2 vector2)
    {
        return new Size(size.Width + vector2.X, size.Height + vector2.Y);
    }

    public static Size operator -(Size size, Vector2 vector2)
    {
        return new Size(size.Width - vector2.X, size.Height - vector2.Y);
    }

    public static Size operator +(Size size, Margin margin)
    {
        return new Size(size.Width + margin.Left + margin.Right, size.Height + margin.Top + margin.Bottom);
    }

    public static Size operator -(Size size, Margin margin)
    {
        return new Size(size.Width - margin.Left - margin.Right, size.Height - margin.Top - margin.Bottom);
    }

    public static Size operator *(Size size, float scale)
    {
        return new Size(size.Width * scale, size.Height * scale);
    }

    public static Size operator /(Size size, float scale)
    {
        return new Size(size.Width / scale, size.Height / scale);
    }

    public static Size operator *(Size size, Size scale)
    {
        return new Size(size.Width * scale.Width, size.Height * scale.Height);
    }

    public static Size operator /(Size size, Size scale)
    {
        return new Size(size.Width / scale.Width, size.Height / scale.Height);
    }

    public static bool operator ==(Size size1, Size size2) => size1.Equals(size2);
    public static bool operator !=(Size size1, Size size2) => !size1.Equals(size2);

    public readonly override bool Equals(object obj) => obj is Size size && Equals(size);
    public readonly bool Equals(Size other) => Width.Equals(other.Width) && Height.Equals(other.Height);

    public readonly override int GetHashCode() => HashCode.Combine(Width, Height);

    public static Size Min(Size size1, Size size2)
    {
        return new Size(MathHelper.Min(size1.Width, size2.Width), MathHelper.Min(size1.Height, size2.Height));
    }

    public static Size Max(Size size1, Size size2)
    {
        return new Size(MathHelper.Max(size1.Width, size2.Width), MathHelper.Max(size1.Height, size2.Height));
    }

    public static Size Clamp(Size value, Size min, Size max)
    {
        return new Size(MathHelper.Clamp(value.Width, min.Width, max.Width),
            MathHelper.Clamp(value.Height, min.Height, max.Height));
    }

    public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

    public string ToString(IFormatProvider provider) => ToString(null, provider);

    /// <summary> 始终按宽、高输出两个数字；provider 为 null 时使用当前文化。 </summary>
    public string ToString(string format, IFormatProvider formatProvider) =>
        $"{Width.ToString(format, formatProvider)} {Height.ToString(format, formatProvider)}";

    public static Size Parse(string s, IFormatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(s);

        return TryParse(s, provider, out var result) ? result :
            throw new FormatException($"Cannot parse '{s}' as Size.");
    }

    /// <summary>
    /// 解析一个或两个空白分隔的裸数字：单值用于宽高，两值按宽、高顺序读取。
    /// 不接受 x 分隔或单位后缀。数字允许正负号、小数和科学计数法，但必须有限。
    /// provider 为 null 时使用当前文化。
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out Size result)
    {
        result = default;
        return s is not null && TryParseCore(s.AsSpan(), provider, out result);
    }

    private static bool TryParseCore(ReadOnlySpan<char> text, IFormatProvider provider, out Size result)
    {
        result = default;

        var remaining = text.Trim();
        if (remaining.IsEmpty) return false;

        Span<float> values = stackalloc float[2];
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

        result = count == 1 ? new Size(values[0]) : new Size(values[0], values[1]);
        return true;
    }
}