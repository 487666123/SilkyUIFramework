namespace SilkyUIFramework;

/// <summary>
/// 单轴（宽或高）的约束与计算结果缓存。
/// </summary>
public struct AxisMetrics
{
    /// <summary>声明尺寸约束（对应 Width/Height 维度）。</summary>
    public float Min, Max, Value;

    /// <summary>内容盒（Inner）约束范围。</summary>
    public float MinInner, MaxInner;

    /// <summary>外盒（Outer）约束范围。</summary>
    public float MinOuter, MaxOuter;

    /// <summary>按 Inner 约束钳制。</summary>
    public readonly float ClampInner(float value) => MathHelper.Clamp(value, MinInner, MaxInner);

    /// <summary>按 Outer 约束钳制。</summary>
    public readonly float ClampOuter(float value) => MathHelper.Clamp(value, MinOuter, MaxOuter);

    /// <summary>
    /// 以声明约束更新 Value
    /// </summary>
    public void SetValueClamped(float value) => Value = MathHelper.Clamp(value, Min, Max);

    /// <summary>
    /// 根据 BoxSizing 与边距参数，计算当前轴在声明/Inner/Outer 三套约束下的最小/最大值。
    /// </summary>
    public void UpdateConstraints(
        Dimension minDimension, Dimension maxDimension, float availableSize,
        BoxSizing boxSizing, float paddingSpan, float border, float marginSpan)
    {
        switch (boxSizing)
        {
            default:
            case BoxSizing.Border:
                Min = Math.Max(minDimension.CalculateSize(availableSize), paddingSpan + border * 2);
                Max = Math.Max(maxDimension.CalculateSize(availableSize), paddingSpan + border * 2);
                MinInner = Min - border * 2 - paddingSpan;
                MaxInner = Max - border * 2 - paddingSpan;
                MinOuter = Min + marginSpan;
                MaxOuter = Max + marginSpan;
                break;
            case BoxSizing.Content:
                Min = minDimension.CalculateSize(availableSize);
                Max = maxDimension.CalculateSize(availableSize);
                MinInner = Min;
                MaxInner = Max;
                MinOuter = Min + border * 2 + paddingSpan + marginSpan;
                MaxOuter = Max + border * 2 + paddingSpan + marginSpan;
                break;
        }
    }
}
