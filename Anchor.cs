using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilkyUIFramework;

/// <summary>
/// 表示一个定位锚点，包含像素偏移、百分比偏移和对齐比例
/// </summary>
public readonly struct Anchor(float pixels = 0f, float percent = 0f, float alignment = 0f) : IEquatable<Anchor>, IParsable<Anchor>
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
    public static bool operator ==(Anchor left, Anchor right) => left.Equals(right);
    public static bool operator !=(Anchor left, Anchor right) => !left.Equals(right);

    public bool Equals(Anchor other) =>
        Pixels.Equals(other.Pixels) &&
        Percent.Equals(other.Percent) &&
        Alignment.Equals(other.Alignment);

    public override bool Equals(object obj) => obj is Anchor other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Pixels, Percent, Alignment);

    public override string ToString() => $"{Pixels}px, {Percent * 100f}%, {Alignment * 100f}#";

    public static Anchor Parse(string s, IFormatProvider provider)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException("Invalid anchor format. Expected: '<number>px [<number>% [<number>#]]'");
        return result;
    }

    public static bool TryParse(string s, IFormatProvider provider, out Anchor result)
    {
        result = default;

        ArgumentNullException.ThrowIfNull(s);

        if (string.IsNullOrWhiteSpace(s))
            return false;

        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (parts.Length)
        {
            case 1:
                return TryParseSingle(parts[0], provider, out result);

            case 3:
                if (TryParseWithSuffix(parts[0], "px", provider, out var px) &&
                    TryParseWithSuffix(parts[1], "%", provider, out var percent) &&
                    TryParseWithSuffix(parts[2], "#", provider, out var align))
                {
                    result = new Anchor(px, percent / 100f, align / 100f);
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    private static bool TryParseSingle([NotNullWhen(true)] string part, IFormatProvider provider, out Anchor result)
    {
        result = default;

        if (part.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
            TryParseWithSuffix(part, "px", provider, out var px))
        {
            result = new Anchor(px);
            return true;
        }

        if (part.EndsWith("%", StringComparison.OrdinalIgnoreCase) &&
            TryParseWithSuffix(part, "%", provider, out var percent))
        {
            result = new Anchor(0f, percent / 100f);
            return true;
        }

        if (part.EndsWith("#", StringComparison.OrdinalIgnoreCase) &&
            TryParseWithSuffix(part, "#", provider, out var align))
        {
            result = new Anchor(0f, 0f, align / 100f);
            return true;
        }

        return false;
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