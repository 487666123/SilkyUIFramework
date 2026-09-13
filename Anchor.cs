using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilkyUIFramework;

/// <summary>
/// 表示一个定位锚点，包含像素偏移、百分比偏移和对齐比例
/// </summary>
public readonly struct Anchor(float pixels = 0f, float percent = 0f, float alignment = 0f) : IEquatable<Anchor>, IParsable<Anchor>, IFormattable
{
    public float Pixels { get; } = pixels;
    public float Percent { get; } = percent;
    public float Alignment { get; } = alignment;

    public Anchor With(float? pixels = null, float? percent = null, float? alignment = null)
    {
        return new Anchor(pixels ?? Pixels, percent ?? Percent, alignment ?? Alignment);
    }

    /// <summary>
    /// 计算最终位置（需容器尺寸和对齐轴长度）
    /// </summary>
    /// <param name="availableSize">容器在对应轴的长度</param>
    /// <param name="elementSize">元素自身的尺寸（用于对齐计算）</param>
    public float CalculatePosition(float availableSize, float elementSize = 0)
    {
        // 基础偏移 = 绝对偏移 + 容器尺寸的百分比偏移
        var baseOffset = Pixels + availableSize * Percent;
        // 对齐调整 = (容器尺寸 - 元素尺寸) * 对齐比例
        var alignmentOffset = (availableSize - elementSize) * Alignment;

        return baseOffset + alignmentOffset;
    }

    //========= 运算符重载 =========//
    public static Anchor Lerp(Anchor a, Anchor b, float t) =>
        new(MathHelper.Lerp(a.Pixels, b.Pixels, t),
            MathHelper.Lerp(a.Percent, b.Percent, t),
            MathHelper.Lerp(a.Alignment, b.Alignment, t));

    public static bool operator ==(Anchor left, Anchor right) => left.Equals(right);
    public static bool operator !=(Anchor left, Anchor right) => !left.Equals(right);

    public bool Equals(Anchor other) =>
        Pixels.Equals(other.Pixels) &&
        Percent.Equals(other.Percent) &&
        Alignment.Equals(other.Alignment);

    public override bool Equals(object obj) => obj is Anchor other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Pixels, Percent, Alignment);

    public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

    public string ToString(IFormatProvider provider) => ToString(null, provider);

    /// <summary> 始终按 px、%、# 顺序输出；provider 为 null 时使用当前文化。 </summary>
    public string ToString(string format, IFormatProvider formatProvider) =>
        $"{Pixels.ToString(format, formatProvider)}px {(Percent * 100f).ToString(format, formatProvider)}% {(Alignment * 100f).ToString(format, formatProvider)}#";

    public static Anchor Parse(string s, IFormatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(s);

        return TryParse(s, provider, out var result) ? result :
            throw new FormatException($"Cannot parse '{s}' as Anchor.");
    }

    /// <summary>
    /// 解析任意顺序的 1～3 个空白分隔分量，例如 50# -20px 100%。
    /// 单位必须紧贴数字，px 不区分大小写，每种单位最多出现一次；% 和 # 均按百分数读取。
    /// 数字允许正负号、小数和科学计数法，但必须有限；provider 为 null 时使用当前文化。
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out Anchor result)
    {
        result = default;
        return s is not null && TryParseCore(s.AsSpan(), provider, out result);
    }

    private static bool TryParseCore(ReadOnlySpan<char> text, IFormatProvider provider, out Anchor result)
    {
        result = default;

        var remaining = text.Trim();
        if (remaining.IsEmpty) return false;

        var pixels = 0f;
        var percent = 0f;
        var alignment = 0f;
        var hasPixels = false;
        var hasPercent = false;
        var hasAlignment = false;

        while (!remaining.IsEmpty)
        {
            var length = 0;
            while (length < remaining.Length && !char.IsWhiteSpace(remaining[length]))
            {
                length++;
            }

            var token = remaining[..length];
            remaining = remaining[length..].TrimStart();

            if (TryParseWithSuffix(token, "px", provider, out var px))
            {
                if (hasPixels) return false;
                pixels = px;
                hasPixels = true;
            }
            else if (TryParseWithSuffix(token, "%", provider, out var percentValue))
            {
                if (hasPercent) return false;
                percent = percentValue / 100f;
                hasPercent = true;
            }
            else if (TryParseWithSuffix(token, "#", provider, out var alignmentValue))
            {
                if (hasAlignment) return false;
                alignment = alignmentValue / 100f;
                hasAlignment = true;
            }
            else
            {
                return false;
            }
        }

        // 完整输入通过检查后才生成结果，失败时始终输出 default。
        result = new Anchor(pixels, percent, alignment);
        return true;
    }

    private static bool TryParseWithSuffix(ReadOnlySpan<char> input, ReadOnlySpan<char> suffix,
        IFormatProvider provider, out float value)
    {
        value = 0f;
        if (!input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return false;

        return float.TryParse(input[..^suffix.Length], NumberStyles.Float, provider, out value) &&
               float.IsFinite(value);
    }
}