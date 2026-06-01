using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilkyUIFramework;

public readonly struct Dimension(float pixels = 0f, float percent = 0f) : IEquatable<Dimension>, IParsable<Dimension>
{
    public float Pixels { get; } = pixels;
    public float Percent { get; } = percent;

    public float CalculateSize(float containerSize) => Pixels + containerSize * Percent;

    public Dimension With(float? pixels = null, float? percent = null) =>
        new(pixels ?? Pixels, percent ?? Percent);

    public static Dimension Lerp(Dimension a, Dimension b, float t) =>
        new(MathHelper.Lerp(a.Pixels, b.Pixels, t),
            MathHelper.Lerp(a.Percent, b.Percent, t));

    public static bool operator ==(Dimension left, Dimension right) => left.Equals(right);
    public static bool operator !=(Dimension left, Dimension right) => !left.Equals(right);

    public bool Equals(Dimension other) => Pixels.Equals(other.Pixels) && Percent.Equals(other.Percent);
    public override bool Equals(object obj) => obj is Dimension other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Pixels, Percent);

    public override string ToString() => $"{Pixels}px {Percent * 100f}%";

    // Parse 调用 TryParse
    public static Dimension Parse(string s, IFormatProvider provider)
    {
        return !TryParse(s, provider, out var result)
            ? throw new FormatException($"Cannot parse '{s}' as Dimension.")
            : result;
    }

    // TryParse 负责核心逻辑
    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out Dimension result)
    {
        result = default;

        ArgumentNullException.ThrowIfNull(s);

        if (string.IsNullOrWhiteSpace(s)) return false;

        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        switch (parts.Length)
        {
            case 1:
                return TryParseSingle(parts[0], provider, out result);

            case 2:
                if (!TryParseWithSuffix(parts[0], "px", provider, out var px) ||
                    !TryParseWithSuffix(parts[1], "%", provider, out var percent)) return false;
                result = new Dimension(px, percent / 100f);
                return true;

            default:
                return false;
        }
    }

    private static bool TryParseSingle(string part, IFormatProvider provider, out Dimension result)
    {
        result = default;

        if (part.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
            TryParseWithSuffix(part, "px", provider, out var px))
        {
            result = new Dimension(px);
            return true;
        }

        if (!part.EndsWith("%", StringComparison.OrdinalIgnoreCase) ||
            !TryParseWithSuffix(part, "%", provider, out var percent)) return false;
        result = new Dimension(0f, percent / 100f);
        return true;
    }

    private static bool TryParseWithSuffix(string input, string suffix, IFormatProvider provider, out float value)
    {
        value = 0f;
        if (!input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return false;

        var numberPart = input[..^suffix.Length];
        return float.TryParse(numberPart, NumberStyles.Float, provider, out value);
    }
}