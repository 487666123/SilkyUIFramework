namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>确定哪些区域属于路径填充范围。</summary>
public enum PathFillRule
{
    /// <summary>填充有向穿越次数之和非零的区域。</summary>
    NonZero,
    /// <summary>填充路径边界穿越次数为奇数的区域。</summary>
    EvenOdd,
    /// <summary>在裁剪范围内填充非零环绕规则的外部区域。</summary>
    InverseNonZero,
    /// <summary>在裁剪范围内填充奇偶规则的外部区域。</summary>
    InverseEvenOdd,
}

/// <summary>指定闭合轮廓在屏幕坐标中的行进方向。</summary>
public enum PathDirection
{
    /// <summary>沿顺时针方向。</summary>
    Clockwise,
    /// <summary>沿逆时针方向。</summary>
    CounterClockwise,
}

/// <summary>可重复绘制、不随构建器改变的二维路径。</summary>
public sealed class VectorPath
{
    private readonly PathCommand[] _commands;

    internal ReadOnlySpan<PathCommand> Commands => _commands;

    /// <summary>获取路径的填充规则。</summary>
    public PathFillRule FillRule { get; }

    /// <summary>获取路径是否完全没有绘制命令。</summary>
    public bool IsEmpty => _commands.Length == 0;

    internal VectorPath(PathCommand[] commands, PathFillRule fillRule)
    {
        _commands = commands;
        FillRule = fillRule;
    }

    /// <summary>解析 SVG 的 <c>path</c> 元素的 <c>d</c> 属性值。</summary>
    /// <param name="data">SVG 路径命令字符串。</param>
    /// <param name="fillRule">解析出的路径使用的填充规则。</param>
    /// <returns>包含解析后轮廓的不可变路径。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="FormatException">命令、参数或弧标志的格式无效。</exception>
    public static VectorPath ParseSvgPathData(string data, PathFillRule fillRule = PathFillRule.NonZero)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new SvgPathData(data, fillRule).Parse();
    }

    /// <summary>对路径应用二维仿射变换，保留其曲线类型和填充规则。</summary>
    /// <param name="matrix">变换路径坐标的二维仿射矩阵。</param>
    /// <returns>变换后的新路径。</returns>
    /// <exception cref="ArgumentException">矩阵或变换后的坐标无效。</exception>
    public VectorPath Transform(Matrix matrix) => new VectorPathBuilder { FillRule = FillRule }
        .AddPath(this, matrix).Build();
}

internal enum PathVerb { Move, Line, Quadratic, Conic, Cubic, Close }

internal readonly record struct PathCommand(PathVerb Verb, Vector2 End = default,
    Vector2 Control1 = default, Vector2 Control2 = default, float Weight = 1f, bool Smooth = false);
