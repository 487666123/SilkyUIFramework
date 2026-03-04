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
        Dimension min, Dimension max, float available,
        BoxSizing boxSizing, float padding, float border, float margin)
    {
        var sum = padding + border * 2;

        switch (boxSizing)
        {
            default:
            case BoxSizing.Border:
                Min = min.CalculateSize(available);
                Max = max.CalculateSize(available);
                MinInner = Min - sum;
                MaxInner = Max - sum;
                MinOuter = Min + margin;
                MaxOuter = Max + margin;
                break;
            case BoxSizing.Content:
                MinInner = min.CalculateSize(available);
                MaxInner = max.CalculateSize(available);
                Min = MinInner + sum;
                Max = MaxInner + sum;
                MinOuter = Min + margin;
                MaxOuter = Max + margin;
                break;
        }
    }
}
