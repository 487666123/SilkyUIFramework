using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilkyUIFramework;

public readonly struct Dimension(float pixels = 0f, float percent = 0f) : IEquatable<Dimension>, IParsable<Dimension>, IFormattable
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

    public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

    public string ToString(IFormatProvider provider) => ToString(null, provider);

    /// <summary> 始终按 px、% 顺序输出；数字格式应用于两个分量，provider 为 null 时使用当前文化。 </summary>
    public string ToString(string format, IFormatProvider formatProvider) =>
        $"{Pixels.ToString(format, formatProvider)}px {(Percent * 100f).ToString(format, formatProvider)}%";

    // Parse 调用 TryParse，空值和格式错误分别抛出对应异常。
    public static Dimension Parse(string s, IFormatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(s);

        return TryParse(s, provider, out var result) ? result :
            throw new FormatException($"Cannot parse '{s}' as Dimension.");
    }

    /// <summary>
    /// 解析一个或两个以空白分隔的分量，例如 20px、50%、50% -20px。
    /// 单位必须紧贴数字，px 不区分大小写，每种单位最多出现一次。
    /// 数字允许正负号、小数和科学计数法，但必须有限；provider 为 null 时使用当前文化。
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out Dimension result)
    {
        result = default;
        return s is not null && TryParseCore(s.AsSpan(), provider, out result);
    }

    private static bool TryParseCore(ReadOnlySpan<char> text, IFormatProvider provider, out Dimension result)
    {
        result = default;

        var remaining = text.Trim();
        if (remaining.IsEmpty) return false;

        var pixels = 0f;
        var percent = 0f;
        var hasPixels = false;
        var hasPercent = false;

        while (!remaining.IsEmpty)
        {
            // 只切分原字符串的视图，不为分量或数字创建子字符串。
            var length = 0;
            while (length < remaining.Length && !char.IsWhiteSpace(remaining[length]))
            {
                length++;
            }

            var token = remaining[..length];
            remaining = remaining[length..].TrimStart();

            bool isPercent;
            ReadOnlySpan<char> number;
            if (token.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                if (hasPixels) return false;
                isPercent = false;
                number = token[..^2];
            }
            else if (token.EndsWith("%", StringComparison.Ordinal))
            {
                if (hasPercent) return false;
                isPercent = true;
                number = token[..^1];
            }
            else
            {
                return false;
            }

            if (!float.TryParse(number, NumberStyles.Float, provider, out var value) ||
                !float.IsFinite(value)) return false;

            if (isPercent)
            {
                percent = value / 100f;
                hasPercent = true;
            }
            else
            {
                pixels = value;
                hasPixels = true;
            }
        }

        // 完整输入通过检查后才生成结果，失败时始终输出 default。
        result = new Dimension(pixels, percent);
        return true;
    }
}