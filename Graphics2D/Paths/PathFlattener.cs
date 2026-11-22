namespace SilkyUIFramework.Graphics2D.Paths;

internal readonly record struct PathPoint(Vector2 Position, bool Corner = true);
internal sealed record PathContour(List<PathPoint> Points, bool Closed);

internal sealed class PathFlattener(Matrix transform)
{
    private const double ToleranceSquared = 0.125 * 0.125;

    private const int MaxDepth = 16;

    private readonly Matrix _transform = transform;

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

    // 用 de Casteljau 递归细分。投影后的控制点靠近端点之间的线段时才停止。
    // 有理曲线在齐次坐标中细分，圆弧保持原来的权重。
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
        Quad(points, a, ab, middle, depth + 1, false);
        Quad(points, middle, bc, c, depth + 1, corner);
    }

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

    private bool IsFlat(HomogeneousPoint a, HomogeneousPoint b, HomogeneousPoint end)
    {
        var (dx, dy) = TransformDelta(a, end);
        var (x, y) = TransformDelta(a, b);
        var length = dx * dx + dy * dy;

        var t = length == 0d ? 0d : Math.Clamp((x * dx + y * dy) / length, 0d, 1d);
        x -= t * dx;
        y -= t * dy;
        return x * x + y * y <= ToleranceSquared;
    }

    private (double X, double Y) TransformDelta(HomogeneousPoint a, HomogeneousPoint b)
    {
        var x = b.X / b.Weight - a.X / a.Weight;

        var y = b.Y / b.Weight - a.Y / a.Weight;
        return (x * _transform.M11 + y * _transform.M21, x * _transform.M12 + y * _transform.M22);
    }

    private readonly record struct HomogeneousPoint(double X, double Y, double Weight)
    {
        public HomogeneousPoint(Vector2 point, double weight = 1d) : this(point.X * weight, point.Y * weight, weight) { }

        public Vector2 Position => new((float)(X / Weight), (float)(Y / Weight));

        public HomogeneousPoint Halfway(HomogeneousPoint b) => new((X + b.X) * 0.5d,
            (Y + b.Y) * 0.5d, (Weight + b.Weight) * 0.5d);
    }
}
