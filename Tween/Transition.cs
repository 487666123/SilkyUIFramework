namespace SilkyUIFramework.Tween;

/// <summary>
/// 过渡曲线类型。
/// </summary>
public enum TransitionType
{
    /// <summary>线性插值</summary>
    Linear = 0,
    /// <summary>正弦曲线</summary>
    Sine = 1,
    /// <summary>五次幂曲线</summary>
    Quint = 2,
    /// <summary>四次幂曲线</summary>
    Quart = 3,
    /// <summary>二次幂曲线</summary>
    Quad = 4,
    /// <summary>指数曲线</summary>
    Expo = 5,
    /// <summary>弹性曲线（带衰减振荡）</summary>
    Elastic = 6,
    /// <summary>三次幂曲线</summary>
    Cubic = 7,
    /// <summary>圆弧曲线</summary>
    Circ = 8,
    /// <summary>弹跳曲线（末尾反弹）</summary>
    Bounce = 9,
    /// <summary>回退曲线（超出后回弹）</summary>
    Back = 10,
    /// <summary>弹簧曲线（阻尼振荡衰减至目标）</summary>
    Spring = 11,
}

/// <summary>
/// 各曲线族的缓入方向函数。缓出与缓入缓出方向由 <see cref="Ease"/> 组合子统一生成。
/// </summary>
internal static class Transition
{
    /// <summary><see cref="TransitionType"/> → 对应方法的映射表</summary>
    public static readonly Func<float, float>[] Map =
    [
        Linear,
        Sine,
        Quint,
        Quart,
        Quad,
        Expo,
        Elastic,
        Cubic,
        Circ,
        Bounce,
        Back,
        Spring,
    ];

    /// <summary>根据 <see cref="TransitionType"/> 获取对应方法</summary>
    public static Func<float, float> Get(TransitionType type) => Map[(int)type];

    /// <summary>线性插值</summary>
    public static float Linear(float t) => t;

    #region 幂函数族

    /// <summary><c>t²</c></summary>
    public static float Quad(float t) => t * t;

    /// <summary><c>t³</c></summary>
    public static float Cubic(float t) => t * t * t;

    /// <summary><c>t⁴</c></summary>
    public static float Quart(float t) => t * t * t * t;

    /// <summary><c>t⁵</c></summary>
    public static float Quint(float t) => t * t * t * t * t;

    #endregion

    #region 三角函数族

    /// <summary><c>1 − cos(t·π/2)</c></summary>
    public static float Sine(float t) => 1 - MathF.Cos(t * MathF.PI / 2);

    #endregion

    #region 指数 / 圆形

    /// <summary><c>2^(10(t−1))</c></summary>
    public static float Expo(float t) => t == 0 ? 0 : MathF.Pow(2, 10 * (t - 1));

    /// <summary><c>1 − √(1−t²)</c></summary>
    public static float Circ(float t) => 1 - MathF.Sqrt(1 - t * t);

    #endregion

    #region 带回弹的

    /// <summary>超出目标后回弹至终点</summary>
    public static float Back(float t)
    {
        const float c = 1.70158f;
        return (c + 1) * t * t * t - c * t * t;
    }

    /// <summary>带衰减正弦振荡</summary>
    public static float Elastic(float t)
    {
        if (t == 0 || t == 1) return t;
        return -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * (2 * MathF.PI / 3));
    }

    /// <summary>分段抛物线模拟反弹，此为缓出形态，缓入由组合子推导</summary>
    public static float Bounce(float t)
    {
        const float n = 7.5625f;
        const float d = 2.75f;
        if (t < 1 / d) return n * t * t;
        if (t < 2 / d) return n * (t -= 1.5f / d) * t + 0.75f;
        if (t < 2.5f / d) return n * (t -= 2.25f / d) * t + 0.9375f;
        return n * (t -= 2.625f / d) * t + 0.984375f;
    }

    /// <summary><c>1 − e^(−7t)·cos(4.5π·t)</c></summary>
    public static float Spring(float t)
    {
        if (t == 0 || t == 1) return t;
        return 1 - MathF.Exp(-7f * t) * MathF.Cos(4.5f * MathF.PI * t);
    }

    #endregion
}
