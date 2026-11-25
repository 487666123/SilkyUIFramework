namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>折线顶点：路径局部坐标中的折线顶点。Corner 为假时，描边在此平滑连接，不应用拐角样式。</summary>
internal readonly record struct PathPoint(Vector2 Position, bool Corner = true);
/// <summary>路径轮廓：按路径顺序保存展平后的顶点，并记录轮廓是否闭合。</summary>
internal sealed record PathContour(List<PathPoint> Points, bool Closed);

/// <summary>路径展平器：按目标像素精度将曲线展平为折线，输出点仍使用路径局部坐标。</summary>
internal sealed class PathFlattener(Matrix transform)
{
    /// <summary>容差平方：以 1/8 像素为平直度阈值；比较距离平方可省去开方。</summary>
    private const double ToleranceSquared = 0.125 * 0.125;

    /// <summary>最大深度：即使尚未达到像素精度，也在此深度停止，限制细分规模。</summary>
    private const int MaxDepth = 16;

    /// <summary>像素变换：将路径局部坐标的位移换算为像素位移，用于判断细分精度。</summary>
    private readonly Matrix _transform = transform;

    /// <summary>扁平化：将路径转换为局部坐标中的折线轮廓，保留各轮廓的闭合状态。</summary>
    public List<PathContour> Flatten(VectorPath path)
    {
        var contours = new List<PathContour>();

        var points = new List<PathPoint>();

        foreach (var command in path.Commands)
        {
            if (command.Verb == PathVerb.Move)
            {
                if (points.Count > 1)
                    contours.Add(new PathContour(points, false));
                points = [new PathPoint(command.End)];
                continue;
            }

            if (command.Verb == PathVerb.Close)
            {
                if (points.Count > 1)
                    contours.Add(new PathContour(points, true));
                points = [];
                continue;
            }

            var start = new HomogeneousPoint(points[^1].Position);

            var end = new HomogeneousPoint(command.End);
            switch (command.Verb)
            {
                case PathVerb.Line:
                    points.Add(new PathPoint(command.End, !command.Smooth));
                    break;
                case PathVerb.Quadratic:
                case PathVerb.Conic:
                    Quad(points, start, new HomogeneousPoint(command.Control1, command.Weight), end, 0, !command.Smooth);
                    break;
                case PathVerb.Cubic:
                    Cubic(points, start, new HomogeneousPoint(command.Control1),
                        new HomogeneousPoint(command.Control2), end, 0, true);
                    break;
            }
        }
        if (points.Count > 1)
            contours.Add(new PathContour(points, false));
        return contours;
    }

    /// <summary>二次曲线细分：在齐次坐标中递归平分二次曲线，直到满足像素容差或达到深度上限；同样适用于带权重的圆弧。</summary>
    private void Quad(List<PathPoint> points, HomogeneousPoint a, HomogeneousPoint b,
        HomogeneousPoint c, int depth, bool corner)
    {
        if (depth == MaxDepth || IsFlat(a, b, c))
        {
            points.Add(new PathPoint(c.Position, corner));
            return;
        }
        var ab = a.Halfway(b);

        var bc = b.Halfway(c);

        var middle = ab.Halfway(bc);
        // 分割点标记：分割点只是曲线的采样点，不是拐角；原终点的标记传给右半段。
        Quad(points, a, ab, middle, depth + 1, false);
        Quad(points, middle, bc, c, depth + 1, corner);
    }

    /// <summary>三次曲线细分：递归平分三次贝塞尔曲线，直到两个控制点都足够靠近首尾线段，或达到深度上限。</summary>
    private void Cubic(List<PathPoint> points, HomogeneousPoint a, HomogeneousPoint b,
        HomogeneousPoint c, HomogeneousPoint d, int depth, bool corner)
    {
        if (depth == MaxDepth || (IsFlat(a, b, d) && IsFlat(a, c, d)))
        {
            points.Add(new PathPoint(d.Position, corner));
            return;
        }
        var ab = a.Halfway(b);

        var bc = b.Halfway(c);

        var cd = c.Halfway(d);

        var left = ab.Halfway(bc);

        var right = bc.Halfway(cd);

        var middle = left.Halfway(right);
        Cubic(points, a, ab, left, middle, depth + 1, false);
        Cubic(points, middle, right, cd, d, depth + 1, corner);
    }

    /// <summary>平直度判断：判断控制点到首尾线段的距离是否在目标像素容差内。</summary>
    private bool IsFlat(HomogeneousPoint a, HomogeneousPoint b, HomogeneousPoint end)
    {
        var (dx, dy) = TransformDelta(a, end);
        var (x, y) = TransformDelta(a, b);
        var length = dx * dx + dy * dy;

        // 投影范围：投影限制在两端之间，避免把越过端点又折返的曲线误判为直线。
        var t = length == 0d ? 0d : Math.Clamp((x * dx + y * dy) / length, 0d, 1d);
        x -= t * dx;
        y -= t * dy;
        return x * x + y * y <= ToleranceSquared;
    }

    /// <summary>像素位移：计算两点投影到二维平面后的位移，并换算为目标像素单位。</summary>
    private (double X, double Y) TransformDelta(HomogeneousPoint a, HomogeneousPoint b)
    {
        var x = b.X / b.Weight - a.X / a.Weight;

        var y = b.Y / b.Weight - a.Y / a.Weight;
        // 平移抵消：两点作差后平移量抵消，只需用矩阵的线性部分换算像素距离。
        return (x * _transform.M11 + y * _transform.M21, x * _transform.M12 + y * _transform.M22);
    }

    /// <summary>齐次点：以 (x × w, y × w, w) 保存二维点，使曲线细分时能同时对坐标和权重取中点。</summary>
    private readonly record struct HomogeneousPoint(double X, double Y, double Weight)
    {
        /// <summary>齐次坐标构造：将二维点和权重转换为齐次坐标。</summary>
        public HomogeneousPoint(Vector2 point, double weight = 1d) : this(point.X * weight, point.Y * weight, weight) { }

        /// <summary>局部位置：将齐次坐标除以权重，还原为路径中的二维点。</summary>
        public Vector2 Position => new((float)(X / Weight), (float)(Y / Weight));

        /// <summary>齐次中点：对齐次坐标的三个分量分别取平均，得到曲线细分使用的中间控制点。</summary>
        public HomogeneousPoint Halfway(HomogeneousPoint b) => new((X + b.X) * 0.5d,
            (Y + b.Y) * 0.5d, (Weight + b.Weight) * 0.5d);
    }
}
