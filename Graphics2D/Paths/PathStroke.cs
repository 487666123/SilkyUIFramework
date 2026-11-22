namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>指定开放轮廓或虚线段的端帽形状。</summary>
public enum PathLineCap
{
    /// <summary>在端点处平齐截断。</summary>
    Butt,
    /// <summary>在端点处添加半圆端帽。</summary>
    Round,
    /// <summary>沿切线向外延伸半个描边宽度，再平齐截断。</summary>
    Square,
}
/// <summary>指定描边在相邻路径段之间的外侧接缝。</summary>
public enum PathLineJoin
{
    /// <summary>延伸两条外边到交点，超出斜接限制时改用斜角。</summary>
    Miter,
    /// <summary>用圆弧连接两条外边。</summary>
    Round,
    /// <summary>用直线截平两条外边。</summary>
    Bevel,
}

/// <summary>居中描边的样式。宽度和虚线长度以路径坐标为单位，零宽为一个设备像素。</summary>
public sealed class PathStroke
{
    private readonly float[] _dashes;

    /// <summary>获取路径单位中的描边宽度。零表示一个设备像素。</summary>
    public float Width { get; }

    /// <summary>获取开放轮廓和虚线段的端帽形状。</summary>
    public PathLineCap Cap { get; }

    /// <summary>获取相邻路径段之间的接缝形状。</summary>
    public PathLineJoin Join { get; }

    /// <summary>获取斜接尖端距拐点的最大距离与半描边宽度的比值。</summary>
    public float MiterLimit { get; }

    /// <summary>获取各轮廓起点在虚线序列中的偏移量。</summary>
    public float DashOffset { get; }

    /// <summary>获取规范化后的线段和间隔长度序列，空序列表示实线。</summary>
    public ReadOnlySpan<float> Dashes => _dashes;

    /// <summary>创建居中于路径的描边样式。</summary>
    /// <param name="width">有限非负宽度，零表示一个设备像素。</param>
    /// <param name="cap">开放轮廓和虚线段的端帽。</param>
    /// <param name="join">相邻路径段之间的接缝。</param>
    /// <param name="miterLimit">斜接长度限制，须为至少 1 的有限数。</param>
    /// <param name="dashes">交替的实线段和间隔长度，元素均须为有限正数。奇数项序列会重复一次。</param>
    /// <param name="dashOffset">各轮廓起点在虚线序列中的偏移量，可以为负数。</param>
    /// <exception cref="ArgumentOutOfRangeException">数值或枚举参数超出有效范围。</exception>
    public PathStroke(float width, PathLineCap cap = PathLineCap.Butt, PathLineJoin join = PathLineJoin.Miter,
        float miterLimit = 4f, ReadOnlySpan<float> dashes = default, float dashOffset = 0f)
    {
        if (!float.IsFinite(width) || width < 0f) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(cap)) throw new ArgumentOutOfRangeException(nameof(cap));
        if (!Enum.IsDefined(join)) throw new ArgumentOutOfRangeException(nameof(join));
        if (!float.IsFinite(miterLimit) || miterLimit < 1f) throw new ArgumentOutOfRangeException(nameof(miterLimit));
        if (!float.IsFinite(dashOffset)) throw new ArgumentOutOfRangeException(nameof(dashOffset));
        foreach (var distance in dashes)
        {
            if (!float.IsFinite(distance) || distance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(dashes), "虚线间隔必须是有限正数。");
        }

        Width = width;
        Cap = cap;
        Join = join;
        MiterLimit = miterLimit;
        DashOffset = dashOffset;
        _dashes = (dashes.Length & 1) == 0 ? dashes.ToArray() : [.. dashes, .. dashes];
    }
}

internal sealed class PathStroker(PathStroke style, Matrix transform)
{
    private readonly PathStroke _style = style;

    private readonly Matrix _transform = transform;

    private readonly float _radius = style.Width == 0f ? 0.5f : style.Width * 0.5f;

    private readonly List<List<Vector2>> _outlines = [];

    public List<List<Vector2>> Stroke(List<PathContour> contours)
    {
        foreach (var contour in contours)
        {
            var points = RemoveDuplicates(contour.Points, contour.Closed);
            if (_style.Dashes.IsEmpty || points.Count < 2)
                StrokeContour(points, contour.Closed);
            else
                DashContour(points, contour.Closed);
        }
        return _outlines;
    }

    private static List<PathPoint> RemoveDuplicates(List<PathPoint> source, bool closed)
    {
        var points = new List<PathPoint>(source.Count);
        foreach (var point in source)
        {
            if (points.Count == 0 || points[^1].Position != point.Position)
                points.Add(point);
            else if (point.Corner)
                points[^1] = point;
        }
        if (closed && points.Count > 1 && points[0].Position == points[^1].Position)
        {
            if (!points[^1].Corner) points[0] = points[^1];
            points.RemoveAt(points.Count - 1);
        }
        return points;
    }

    private void DashContour(List<PathPoint> points, bool closed)
    {
        var pattern = _style.Dashes;

        double period = 0d;
        foreach (var length in pattern)
            period += length;
        var phase = _style.DashOffset % period;
        if (phase < 0d) phase += period;
        if (phase == period) phase = 0d;
        var index = 0;
        while (index + 1 < pattern.Length && phase >= pattern[index])
            phase -= pattern[index++];
        var remaining = pattern[index] - phase;

        var startsOn = (index & 1) == 0;

        var runs = new List<List<PathPoint>>();

        List<PathPoint> run = null;

        var segmentCount = closed ? points.Count : points.Count - 1;

        var endsOn = false;

        for (int i = 0; i < segmentCount; i++)
        {
            var start = points[i];

            var end = points[(i + 1) % points.Count];

            var length = Length(start.Position, end.Position);

            double distance = 0d;
            while (distance < length)
            {
                var available = length - distance;

                var step = Math.Min(remaining, available);

                var next = step == available ? length : distance + step;
                if (next <= distance)
                    throw new ArgumentException("虚线间隔低于路径坐标精度。", nameof(points));
                endsOn = (index & 1) == 0;
                if (endsOn)
                {
                    if (run is null)
                    {
                        if (runs.Count >= 32768)
                            throw new ArgumentException("虚线产生的轮廓过多。", nameof(points));
                        run = [At(start, end, distance / length)];
                        runs.Add(run);
                    }
                    run.Add(At(start, end, next / length));
                }
                distance = next;
                remaining -= step;
                if (remaining <= 0d)
                {
                    index = (index + 1) % pattern.Length;
                    remaining = pattern[index];
                    run = null;
                }
            }
        }

        // 穿过闭合接缝的同一个虚线段应连接，接缝上没有额外端帽。
        if (closed && startsOn && endsOn)
        {
            if (runs.Count == 1)
            {
                StrokeContour(RemoveDuplicates(runs[0], true), true);
                return;
            }
            var last = runs[^1];
            last.RemoveAt(last.Count - 1);
            last.AddRange(runs[0]);
            runs.RemoveAt(0);
        }
        foreach (var dash in runs)
            StrokeContour(RemoveDuplicates(dash, false), false);
    }

    private static PathPoint At(PathPoint start, PathPoint end, double fraction)
    {
        if (fraction <= 0d) return start;
        if (fraction >= 1d) return end;
        return new PathPoint(new Vector2(
            (float)(start.Position.X + ((double)end.Position.X - start.Position.X) * fraction),
            (float)(start.Position.Y + ((double)end.Position.Y - start.Position.Y) * fraction)), false);
    }

    private void StrokeContour(List<PathPoint> source, bool closed)
    {
        var points = source;
        if (_style.Width == 0f)
        {
            points = [];
            foreach (var point in source)
                points.Add(point with { Position = Vector2.Transform(point.Position, _transform) });
            points = RemoveDuplicates(points, closed);
        }
        if (points.Count == 1)
        {
            var point = points[0].Position;
            if (_style.Cap == PathLineCap.Round || (closed && _style.Join == PathLineJoin.Round))
                AddArc(point, Vector2.UnitX, Vector2.UnitX, Math.Tau);
            else if (!closed && _style.Cap == PathLineCap.Square)
                AddPolygon([point + new Vector2(-_radius, -_radius), point + new Vector2(_radius, -_radius),
                    point + new Vector2(_radius, _radius), point + new Vector2(-_radius, _radius)]);
            return;
        }

        var segmentCount = closed ? points.Count : points.Count - 1;

        var directions = new Vector2[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            var start = points[i].Position;

            var end = points[(i + 1) % points.Count].Position;

            var length = Length(start, end);

            var tangent = new Vector2((float)(((double)end.X - start.X) / length),
                (float)(((double)end.Y - start.Y) / length));
            directions[i] = tangent;
            var normal = Normal(tangent) * _radius;
            if (!closed && _style.Cap == PathLineCap.Square)
            {
                if (i == 0) start -= tangent * _radius;
                if (i == segmentCount - 1) end += tangent * _radius;
            }
            AddPolygon([start + normal, start - normal, end - normal, end + normal]);
        }
        for (int i = closed ? 0 : 1; i < segmentCount; i++)
            AddJoin(points[i], directions[(i + segmentCount - 1) % segmentCount], directions[i]);

        if (!closed && _style.Cap == PathLineCap.Round)
        {
            var start = Normal(directions[0]);

            var end = -Normal(directions[^1]);
            AddArc(points[0].Position, start, -start, Math.PI);
            AddArc(points[^1].Position, end, -end, Math.PI);
        }
    }

    private void AddJoin(PathPoint point, Vector2 before, Vector2 after)
    {
        var cross = (double)before.X * after.Y - (double)before.Y * after.X;

        var dot = (double)before.X * after.X + (double)before.Y * after.Y;

        var join = point.Corner ? _style.Join : PathLineJoin.Round;
        if (Math.Abs(cross) < 1e-7d)
        {
            if (dot < 0d && join == PathLineJoin.Round)
                AddArc(point.Position, -Normal(before), Normal(before), Math.PI);
            return;
        }

        var side = cross > 0d ? -1f : 1f;

        var a = Normal(before) * side;

        var b = Normal(after) * side;

        var center = point.Position;
        if (join == PathLineJoin.Round)
        {
            AddArc(center, a, b, Math.Atan2(cross, dot));
            return;
        }

        // 斜接超出限制时退回斜角。全部轮廓同向，重叠处只绘制一次。
        var scale = 1d + dot;
        if (join == PathLineJoin.Miter && scale > 0d && 2d / scale <= (double)_style.MiterLimit * _style.MiterLimit)
            AddPolygon([center, center + a * _radius, center + (a + b) * (float)(_radius / scale), center + b * _radius]);
        else
            AddPolygon([center, center + a * _radius, center + b * _radius]);
    }

    private void AddArc(Vector2 center, Vector2 start, Vector2 end, double sweep)
    {
        var pixelRadius = _style.Width == 0f ? _radius : _radius * Math.Sqrt(
            (double)_transform.M11 * _transform.M11 + (double)_transform.M12 * _transform.M12 +
            (double)_transform.M21 * _transform.M21 + (double)_transform.M22 * _transform.M22);

        var step = Math.Max(0.01d, 2d * Math.Acos(1d - 0.125d / Math.Max(0.125d, pixelRadius)));

        var count = Math.Max((int)Math.Ceiling(Math.Abs(sweep) / step), (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 2d)));

        var points = new List<Vector2>(count + 2) { center, center + start * _radius };
        for (int i = 1; i < count; i++)
        {
            var (sin, cos) = Math.SinCos(sweep * i / count);
            points.Add(center + _radius * new Vector2((float)(start.X * cos - start.Y * sin),
                (float)(start.X * sin + start.Y * cos)));
        }
        points.Add(center + end * _radius);
        AddPolygon(points);
    }

    private void AddPolygon(List<Vector2> points)
    {
        var origin = points[0];

        double area = 0d;
        for (int i = 1; i + 1 < points.Count; i++)
        {
            var a = points[i];

            var b = points[i + 1];
            area += ((double)a.X - origin.X) * ((double)b.Y - origin.Y) - ((double)a.Y - origin.Y) * ((double)b.X - origin.X);
        }
        if (area < 0d) points.Reverse();
        if (area != 0d) _outlines.Add(points);
    }

    private static Vector2 Normal(Vector2 direction) => new(-direction.Y, direction.X);

    private static double Length(Vector2 a, Vector2 b)
    {
        double x = (double)b.X - a.X, y = (double)b.Y - a.Y;
        return Math.Sqrt(x * x + y * y);
    }
}
