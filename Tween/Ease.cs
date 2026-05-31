namespace SilkyUIFramework.Tween;

/// <summary>
/// 缓动方向。
/// </summary>
public enum EaseType
{
    /// <summary>缓入：起始缓慢，末尾加速</summary>
    In = 0,
    /// <summary>缓出：起始快速，末尾减速</summary>
    Out = 1,
    /// <summary>缓入缓出：两端缓慢，中间加速</summary>
    InOut = 2,
    /// <summary>缓出缓入：两端加速，中间缓慢</summary>
    OutIn = 3,
}

/// <summary>
/// 基于三次贝塞尔的缓动曲线，提供 <c>float → float</c> 纯函数。
/// </summary>
internal static class Ease
{
    /// <summary><see cref="EaseType"/> → 对应方法的映射表</summary>
    public static readonly Func<float, float>[] Map =
    [
        In,
        Out,
        InOut,
        OutIn,
    ];

    /// <summary>根据 <see cref="EaseType"/> 获取对应方法</summary>
    public static Func<float, float> Get(EaseType type) => Map[(int)type];

    /// <summary><c>cubic-bezier(0.42, 0, 1, 1)</c>，起始缓慢末尾加速</summary>
    public static float In(float t) => CubicBezier(t, 0.42f, 0, 1, 1);

    /// <summary><c>cubic-bezier(0, 0, 0.58, 1)</c>，起始快速末尾减速</summary>
    public static float Out(float t) => CubicBezier(t, 0, 0, 0.58f, 1);

    /// <summary><c>cubic-bezier(0.42, 0, 0.58, 1)</c>，两端缓慢中间加速</summary>
    public static float InOut(float t) => CubicBezier(t, 0.42f, 0, 0.58f, 1);

    /// <summary><c>cubic-bezier(0.58, 1, 0.42, 0)</c>，两端加速中间缓慢</summary>
    public static float OutIn(float t) => CubicBezier(t, 0.58f, 1, 0.42f, 0);

    /// <summary>
    /// 三次贝塞尔求解器。给定 <paramref name="t"/> ∈ [0,1] 和控制点 (x1,y1, x2,y2)，
    /// 返回曲线在 x=<paramref name="t"/> 处对应的 y 值。
    /// </summary>
    private static float CubicBezier(float t, float x1, float y1, float x2, float y2)
    {
        // 牛顿法迭代：求 Bx(τ) = t 的 τ，再代入 By(τ)
        var guess = t;
        for (var i = 0; i < 8; i++)
        {
            var x = BezierX(guess, x1, x2) - t;
            if (Math.Abs(x) < 1e-5f) break;
            var dx = BezierDX(guess, x1, x2);
            if (Math.Abs(dx) < 1e-10f) break;
            guess -= x / dx;
        }

        return BezierX(Math.Clamp(guess, 0, 1), y1, y2);
    }

    /// <summary>返回曲线在参数 τ 处的 x 坐标，控制点 x 坐标为 0, a, b, 1</summary>
    private static float BezierX(float t, float a, float b)
        => ((1 + 3 * a - 3 * b) * t + 3 * (b - 2 * a)) * t * t + 3 * a * t;

    /// <summary>返回曲线在参数 τ 处的切线斜率（导数）</summary>
    private static float BezierDX(float t, float a, float b)
        => 3 * (1 + 3 * a - 3 * b) * t * t + 6 * (b - 2 * a) * t + 3 * a;
}
