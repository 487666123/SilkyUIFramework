namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>指定渐变区间之外的着色方式。</summary>
public enum GradientTileMode
{
    /// <summary>延伸两端的颜色。</summary>
    Clamp,
    /// <summary>周期性重复渐变。</summary>
    Repeat,
    /// <summary>交替正向和反向重复渐变。</summary>
    Mirror,
    /// <summary>区间之外为透明。</summary>
    Decal,
}

/// <summary>路径填充和描边共用的不可变渐变画笔。颜色输入为非预乘的 sRGB 色值。</summary>
public sealed class PathGradient
{
    private const int MaxStops = 4096;

    private const float UniformStopTolerance = 1f / 4096f;

    private const float DegenerateThreshold = 1f / 32768f;

    internal GradientTileMode TileMode { get; }

    internal bool IsRadial { get; }

    internal bool Premultiplied { get; }

    internal bool LinearRgb { get; }

    internal Matrix LocalMatrix { get; }

    internal Vector2 Origin { get; }

    internal double X { get; }

    internal double Y { get; }

    internal GradientStop[] Stops { get; }

    private PathGradient(Vector2 origin, double x, double y, bool radial, bool degenerate,
        ReadOnlySpan<Color> colors, ReadOnlySpan<float> positions, GradientTileMode tileMode,
        bool premultiplied, bool linearRgb, Matrix? localMatrix)
    {
        if (colors.IsEmpty || colors.Length > MaxStops - 2)
            throw new ArgumentOutOfRangeException(nameof(colors), "颜色数量须在 1 到 4094 之间。");
        if (!positions.IsEmpty && positions.Length != colors.Length)
            throw new ArgumentException("颜色与停止点位置的数量必须一致。", nameof(positions));
        if (!Enum.IsDefined(tileMode)) throw new ArgumentOutOfRangeException(nameof(tileMode));
        LocalMatrix = localMatrix ?? Matrix.Identity;
        VectorPathBuilder.CheckTransform(LocalMatrix);
        if ((double)LocalMatrix.M11 * LocalMatrix.M22 == (double)LocalMatrix.M12 * LocalMatrix.M21)
            throw new ArgumentException("渐变局部矩阵必须可逆。", nameof(localMatrix));

        var stops = new List<GradientStop>(colors.Length + 2);

        float previous = 0f;
        for (int i = 0; i < colors.Length; i++)
        {
            var position = positions.IsEmpty ? i / (float)Math.Max(colors.Length - 1, 1) : positions[i];
            if (!float.IsFinite(position)) throw new ArgumentOutOfRangeException(nameof(positions));
            previous = Math.Clamp(position, previous, 1f);
            stops.Add(new GradientStop(previous, colors[i].ToVector4()));
        }
        if (stops[0].Position > 0f) stops.Insert(0, new GradientStop(0f, stops[0].Color));
        if (stops[^1].Position < 1f) stops.Add(new GradientStop(1f, stops[^1].Color));

        Origin = origin;
        TileMode = tileMode;
        if (colors.Length == 1 || degenerate)
        {
            // Skia 以非预乘的原始颜色计算退化渐变的周期平均值。
            var solid = colors.Length == 1 ? colors[0].ToVector4() : tileMode switch
            {
                GradientTileMode.Clamp => stops[^1].Color,
                GradientTileMode.Decal => Vector4.Zero,
                _ => Average(stops),
            };
            Stops = [new GradientStop(0f, solid)];
            return;
        }

        X = x;
        Y = y;
        IsRadial = radial;
        Premultiplied = premultiplied;
        LinearRgb = linearRgb;
        // 与 Skia 一样，把近似等距的停止点当作隐式等距序列。
        var step = stops[1].Position - stops[0].Position;
        if (Enumerable.Range(1, stops.Count - 1).All(i =>
                MathF.Abs(stops[i].Position - stops[i - 1].Position - step) <= UniformStopTolerance))
        {
            Stops = new GradientStop[stops.Count];
            for (int i = 0; i < stops.Count; i++)
                Stops[i] = PrepareColor(stops[i] with { Position = i / (float)(stops.Count - 1) });
            return;
        }

        var normalized = new List<GradientStop>(stops.Count);
        for (int first = 0; first < stops.Count;)
        {
            int next = first + 1;

            var left = stops[first];
            while (next < stops.Count && stops[next].Position == left.Position) next++;
            var duplicate = next - first > 1;
            if (!duplicate || tileMode == GradientTileMode.Clamp || left.Position != 0f)
                normalized.Add(PrepareColor(left));
            if (duplicate && (tileMode == GradientTileMode.Clamp || left.Position != 1f))
                normalized.Add(PrepareColor(stops[next - 1]));
            first = next;
        }
        Stops = normalized.ToArray();
    }

    /// <summary>创建沿起点到终点方向变化的线性渐变。</summary>
    /// <param name="start">路径局部坐标中的渐变起点。</param>
    /// <param name="end">路径局部坐标中的渐变终点。</param>
    /// <param name="colors">非预乘 sRGB 颜色序列，数量为 1 到 4094。</param>
    /// <param name="positions">停止点位置。空序列表示均匀分布，其他位置按 Skia 规则钳制到单调的 [0, 1] 区间。</param>
    /// <param name="tileMode">渐变区间之外的着色方式。</param>
    /// <param name="premultiplied">是否在预乘 Alpha 后进行插值。默认先插值再预乘。</param>
    /// <param name="linearRgb">是否在线性 sRGB 中插值。默认在 sRGB 编码值之间插值。</param>
    /// <param name="localMatrix">从渐变坐标到路径坐标的额外可逆二维仿射变换。</param>
    /// <returns>可重复用于填充和描边的画笔。</returns>
    /// <exception cref="ArgumentException">渐变坐标、停止点、平铺模式或局部矩阵无效。</exception>
    public static PathGradient Linear(Vector2 start, Vector2 end, ReadOnlySpan<Color> colors,
        ReadOnlySpan<float> positions = default, GradientTileMode tileMode = GradientTileMode.Clamp,
        bool premultiplied = false, bool linearRgb = false, Matrix? localMatrix = null)
    {
        CheckPoint(start, nameof(start));
        CheckPoint(end, nameof(end));
        var x = (double)end.X - start.X;

        var y = (double)end.Y - start.Y;

        var squaredLength = x * x + y * y;

        var degenerate = squaredLength <= (double)DegenerateThreshold * DegenerateThreshold;
        return new PathGradient(start, degenerate ? 0 : x / squaredLength, degenerate ? 0 : y / squaredLength,
            false, degenerate, colors, positions, tileMode, premultiplied, linearRgb, localMatrix);
    }

    /// <summary>创建随到中心的距离变化的径向渐变。</summary>
    /// <param name="center">路径局部坐标中的圆心。</param>
    /// <param name="radius">路径单位中的有限非负半径。</param>
    /// <param name="colors">非预乘 sRGB 颜色序列，数量为 1 到 4094。</param>
    /// <param name="positions">停止点位置。空序列表示均匀分布，其他位置按 Skia 规则钳制到单调的 [0, 1] 区间。</param>
    /// <param name="tileMode">渐变区间之外的着色方式。</param>
    /// <param name="premultiplied">是否在预乘 Alpha 后进行插值。默认先插值再预乘。</param>
    /// <param name="linearRgb">是否在线性 sRGB 中插值。默认在 sRGB 编码值之间插值。</param>
    /// <param name="localMatrix">从渐变坐标到路径坐标的额外可逆二维仿射变换。可用于设置椭圆渐变。</param>
    /// <returns>可重复用于填充和描边的画笔。</returns>
    /// <exception cref="ArgumentException">渐变坐标、半径、停止点、平铺模式或局部矩阵无效。</exception>
    public static PathGradient Radial(Vector2 center, float radius, ReadOnlySpan<Color> colors,
        ReadOnlySpan<float> positions = default, GradientTileMode tileMode = GradientTileMode.Clamp,
        bool premultiplied = false, bool linearRgb = false, Matrix? localMatrix = null)
    {
        CheckPoint(center, nameof(center));
        if (!float.IsFinite(radius) || radius < 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        var degenerate = radius <= DegenerateThreshold;
        return new PathGradient(center, degenerate ? 0 : 1d / radius, 0d,
            true, degenerate, colors, positions, tileMode, premultiplied, linearRgb, localMatrix);
    }

    private GradientStop PrepareColor(GradientStop stop)
    {
        var color = stop.Color;
        if (LinearRgb)
            color = new Vector4(ToLinear(color.X), ToLinear(color.Y), ToLinear(color.Z), color.W);
        if (Premultiplied)
            color = new Vector4(color.X * color.W, color.Y * color.W, color.Z * color.W, color.W);
        return stop with { Color = color };
    }

    private static float ToLinear(float value) => value <= 0.04045f
        ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);

    private static Vector4 Average(List<GradientStop> stops)
    {
        var sum = Vector4.Zero;
        for (int i = 1; i < stops.Count; i++)
            sum += (stops[i - 1].Color + stops[i].Color) * (0.5f * (stops[i].Position - stops[i - 1].Position));
        return sum;
    }

    private static void CheckPoint(Vector2 point, string name)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentOutOfRangeException(name);
    }

    internal readonly record struct GradientStop(float Position, Vector4 Color);
}
