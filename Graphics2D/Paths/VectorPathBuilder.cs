namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>按轮廓记录路径命令。角度使用弧度，屏幕坐标中的正角为顺时针。</summary>
public sealed class VectorPathBuilder
{
    private readonly List<PathCommand> _commands = [];

    private Vector2 _start;

    private Vector2 _current;

    private bool _open;

    /// <summary>获取或设置下次构建路径时使用的填充规则。</summary>
    public PathFillRule FillRule { get; set; }

    /// <summary>获取当前绘制点。闭合轮廓后，该点回到轮廓起点。</summary>
    public Vector2 CurrentPoint => _current;

    /// <summary>创建当前路径的不可变快照。</summary>
    /// <returns>包含当前轮廓及填充规则的路径。</returns>
    /// <exception cref="ArgumentOutOfRangeException">填充规则不是已定义的值。</exception>
    public VectorPath Build()
    {
        if (!Enum.IsDefined(FillRule))
            throw new ArgumentOutOfRangeException(nameof(FillRule));
        return new VectorPath(_commands.ToArray(), FillRule);
    }

    /// <summary>清空轮廓，将绘制点与填充规则恢复为默认值。</summary>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder Reset()
    {
        _commands.Clear();
        _current = _start = default;
        _open = false;
        FillRule = PathFillRule.NonZero;
        return this;
    }

    /// <summary>从指定位置开始一个新轮廓。</summary>
    /// <param name="point">新轮廓的起点。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder MoveTo(Vector2 point)
    {
        CheckPoint(point);
        if (_open && _commands[^1].Verb == PathVerb.Move)
            _commands[^1] = new PathCommand(PathVerb.Move, point);
        else
            _commands.Add(new PathCommand(PathVerb.Move, point));
        _current = _start = point;
        _open = true;
        return this;
    }

    /// <summary>从当前点添加一条直线。</summary>
    /// <param name="end">线段终点。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder LineTo(Vector2 end) => Add(new PathCommand(PathVerb.Line, end));

    /// <summary>从当前点添加一条二次贝塞尔曲线。</summary>
    /// <param name="control">控制点。</param>
    /// <param name="end">曲线终点。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder QuadTo(Vector2 control, Vector2 end) => Add(new PathCommand(PathVerb.Quadratic, end, control));

    /// <summary>从当前点添加一条三次贝塞尔曲线。</summary>
    /// <param name="control1">起点侧的控制点。</param>
    /// <param name="control2">终点侧的控制点。</param>
    /// <param name="end">曲线终点。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder CubicTo(Vector2 control1, Vector2 control2, Vector2 end) =>
        Add(new PathCommand(PathVerb.Cubic, end, control1, control2));

    /// <summary>从当前点添加一条有理二次贝塞尔曲线。</summary>
    /// <param name="control">控制点。</param>
    /// <param name="end">曲线终点。</param>
    /// <param name="weight">控制点的有限正权重。为 1 时等价于 <see cref="QuadTo"/>。</param>
    /// <returns>当前构建器。</returns>
    /// <exception cref="ArgumentOutOfRangeException">权重或坐标无效。</exception>
    public VectorPathBuilder ConicTo(Vector2 control, Vector2 end, float weight) => AddConic(control, end, weight);

    private VectorPathBuilder AddConic(Vector2 control, Vector2 end, float weight, bool smooth = false)
    {
        if (!float.IsFinite(weight) || weight <= 0f)
            throw new ArgumentOutOfRangeException(nameof(weight));
        var verb = weight == 1f ? PathVerb.Quadratic : PathVerb.Conic;
        return Add(new PathCommand(verb, end, control, Weight: weight, Smooth: smooth));
    }

    /// <summary>闭合当前轮廓，并使当前点回到该轮廓的起点。</summary>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder Close()
    {
        if (_open)
        {
            _commands.Add(new PathCommand(PathVerb.Close));
            _current = _start;
            _open = false;
        }
        return this;
    }

    /// <summary>按原坐标追加路径的轮廓。</summary>
    /// <param name="path">要追加的路径。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder AddPath(VectorPath path) => AddPath(path, Matrix.Identity);

    /// <summary>追加已变换的轮廓，合成路径使用当前构建器的填充规则。</summary>
    /// <param name="path">要追加的路径。</param>
    /// <param name="transform">应用于追加轮廓的二维仿射矩阵。</param>
    /// <returns>当前构建器。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException">变换不是有效的二维仿射变换，或变换后的坐标超出范围。</exception>
    public VectorPathBuilder AddPath(VectorPath path, Matrix transform)
    {
        ArgumentNullException.ThrowIfNull(path);
        CheckTransform(transform);
        foreach (var command in path.Commands)
        {
            switch (command.Verb)
            {
                case PathVerb.Move:
                    MoveTo(Vector2.Transform(command.End, transform));
                    break;
                case PathVerb.Close:
                    Close();
                    break;
                default:
                    Add(command with
                    {
                        End = Vector2.Transform(command.End, transform),
                        Control1 = command.Verb == PathVerb.Line ? default : Vector2.Transform(command.Control1, transform),
                        Control2 = command.Verb == PathVerb.Cubic ? Vector2.Transform(command.Control2, transform) : default,
                    });
                    break;
            }
        }
        return this;
    }

    /// <summary>添加闭合矩形轮廓。</summary>
    /// <param name="position">矩形左上角。</param>
    /// <param name="size">矩形的宽和高，均须为非负数。</param>
    /// <param name="direction">轮廓方向。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder AddRect(Vector2 position, Vector2 size, PathDirection direction = PathDirection.Clockwise)
    {
        CheckSize(size);
        var x = new Vector2(size.X, 0);

        var y = new Vector2(0, size.Y);
        return direction == PathDirection.Clockwise
            ? MoveTo(position).LineTo(position + x).LineTo(position + size).LineTo(position + y).Close()
            : MoveTo(position).LineTo(position + y).LineTo(position + size).LineTo(position + x).Close();
    }

    /// <summary>添加闭合的圆形轮廓。</summary>
    /// <param name="center">圆心。</param>
    /// <param name="radius">半径，须为非负数。</param>
    /// <param name="direction">轮廓方向。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder AddCircle(Vector2 center, float radius, PathDirection direction = PathDirection.Clockwise) =>
        AddEllipse(center, new Vector2(radius), direction);

    /// <summary>添加闭合的椭圆轮廓。</summary>
    /// <param name="center">椭圆中心。</param>
    /// <param name="radii">水平和垂直半径，均须为非负数。</param>
    /// <param name="direction">轮廓方向。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder AddEllipse(Vector2 center, Vector2 radii, PathDirection direction = PathDirection.Clockwise) =>
        ArcTo(center, radii, 0f, direction == PathDirection.Clockwise ? MathF.Tau : -MathF.Tau, moveToStart: true).Close();

    /// <summary>添加闭合的圆角矩形轮廓。</summary>
    /// <param name="position">矩形左上角。</param>
    /// <param name="size">矩形的宽和高，均须为非负数。</param>
    /// <param name="radii">圆角的水平和垂直半径，分别限制为对应边长的一半。</param>
    /// <param name="direction">轮廓方向。</param>
    /// <returns>当前构建器。</returns>
    public VectorPathBuilder AddRoundRect(Vector2 position, Vector2 size, Vector2 radii,
        PathDirection direction = PathDirection.Clockwise)
    {
        CheckSize(size);
        CheckSize(radii);
        radii = Vector2.Min(radii, size * 0.5f);
        if (radii.X == 0f || radii.Y == 0f)
            return AddRect(position, size, direction);

        var insetLeft = position.X + radii.X;

        var insetRight = position.X + (size.X - radii.X);

        var insetTop = position.Y + radii.Y;

        var insetBottom = position.Y + (size.Y - radii.Y);

        var right = position.X + size.X;

        var bottom = position.Y + size.Y;

        var topLeft = new Vector2(insetLeft, position.Y);

        var topRight = new Vector2(insetRight, position.Y);

        var rightTop = new Vector2(right, insetTop);

        var rightBottom = new Vector2(right, insetBottom);

        var bottomRight = new Vector2(insetRight, bottom);

        var bottomLeft = new Vector2(insetLeft, bottom);

        var leftBottom = new Vector2(position.X, insetBottom);

        var leftTop = new Vector2(position.X, insetTop);

        const float quarterWeight = 0.7071067812f;

        MoveTo(direction == PathDirection.Clockwise ? topLeft : topRight);
        if (direction == PathDirection.Clockwise)
            return ConnectTo(topRight).AddConic(new Vector2(right, position.Y), rightTop, quarterWeight, true)
                .ConnectTo(rightBottom).AddConic(new Vector2(right, bottom), bottomRight, quarterWeight, true)
                .ConnectTo(bottomLeft).AddConic(new Vector2(position.X, bottom), leftBottom, quarterWeight, true)
                .ConnectTo(leftTop).AddConic(position, topLeft, quarterWeight, true).Close();

        return ConnectTo(topLeft).AddConic(position, leftTop, quarterWeight, true)
            .ConnectTo(leftBottom).AddConic(new Vector2(position.X, bottom), bottomLeft, quarterWeight, true)
            .ConnectTo(bottomRight).AddConic(new Vector2(right, bottom), rightBottom, quarterWeight, true)
            .ConnectTo(rightTop).AddConic(new Vector2(right, position.Y), topRight, quarterWeight, true).Close();
    }

    /// <summary>按中心、起始角和扫过角添加椭圆弧。</summary>
    /// <param name="center">椭圆中心。</param>
    /// <param name="radii">水平和垂直半径，均须为非负数。</param>
    /// <param name="startAngle">椭圆自身坐标中的起始角，单位为弧度。</param>
    /// <param name="sweepAngle">扫过的角度，单位为弧度，绝对值至多为一整圈。</param>
    /// <param name="rotation">椭圆相对路径坐标轴旋转的弧度。</param>
    /// <param name="moveToStart">为 <see langword="true"/> 时在弧起点开始新轮廓，否则用直线接入。</param>
    /// <returns>当前构建器。</returns>
    /// <exception cref="ArgumentOutOfRangeException">坐标、半径或角度超出有效范围。</exception>
    public VectorPathBuilder ArcTo(Vector2 center, Vector2 radii, float startAngle, float sweepAngle,
        float rotation = 0f, bool moveToStart = false)
    {
        CheckPoint(center);
        CheckSize(radii);
        if (!float.IsFinite(startAngle) || !float.IsFinite(rotation))
            throw new ArgumentOutOfRangeException(nameof(startAngle));
        if (!float.IsFinite(sweepAngle) || MathF.Abs(sweepAngle) > MathF.Tau)
            throw new ArgumentOutOfRangeException(nameof(sweepAngle));

        var arc = new Ellipse(center.X, center.Y, radii.X, radii.Y, rotation);

        var angle = SnapQuarterTurn(startAngle);
        if (Math.Abs(angle) > Math.Tau)
        {
            var (sin, cos) = Math.SinCos(angle);
            angle = Math.Atan2(sin, cos);
        }
        var start = arc.Point(angle);
        if (moveToStart || _commands.Count == 0)
            MoveTo(start);
        else if (_current != start)
            LineTo(start);

        var full = MathF.Abs(sweepAngle) == MathF.Tau;

        double sweep = SnapQuarterTurn(sweepAngle);
        return AppendArc(arc, angle, sweep, full ? start : arc.Point(angle + sweep), full);
    }

    /// <summary>按 SVG 的端点参数约定追加旋转椭圆弧。</summary>
    /// <param name="radii">椭圆的水平和垂直半径，使用绝对值；任一半径为零时追加直线。</param>
    /// <param name="rotation">椭圆相对路径坐标轴旋转的弧度。</param>
    /// <param name="largeArc">为 <see langword="true"/> 时选择跨角较大的弧。</param>
    /// <param name="clockwise">为 <see langword="true"/> 时沿顺时针方向绘制。</param>
    /// <param name="end">椭圆弧终点。与当前点相同时省略该段。</param>
    /// <returns>当前构建器。</returns>
    /// <exception cref="ArgumentOutOfRangeException">坐标、半径或旋转角超出有效范围。</exception>
    public VectorPathBuilder ArcTo(Vector2 radii, float rotation, bool largeArc, bool clockwise, Vector2 end)
    {
        CheckPoint(end);
        CheckPoint(radii);
        if (!float.IsFinite(rotation))
            throw new ArgumentOutOfRangeException(nameof(rotation));
        if (_current == end)
            return this;
        if (radii.X == 0f || radii.Y == 0f)
            return LineTo(end);

        var rx = Math.Abs((double)radii.X);

        var ry = Math.Abs((double)radii.Y);
        var (sin, cos) = Math.SinCos(rotation);
        var dx = ((double)_current.X - end.X) / 2d;

        var dy = ((double)_current.Y - end.Y) / 2d;

        var x = (cos * dx + sin * dy) / rx;

        var y = (-sin * dx + cos * dy) / ry;

        var length = Math.Sqrt(x * x + y * y);
        if (length > 1d)
        {
            rx *= length;
            ry *= length;
            x /= length;
            y /= length;
        }

        // 先在单位圆求圆心与角度，再映射回旋转后的椭圆。
        var factor = Math.Sqrt(Math.Max(0d, 1d - length * length)) / length;
        if (largeArc == clockwise)
            factor = -factor;
        var cx = factor * y;

        var cy = -factor * x;

        var start = Math.Atan2(y - cy, x - cx);

        var minorSweep = 2d * Math.Asin(Math.Min(length, 1d));

        var sweep = (largeArc ? Math.Tau - minorSweep : minorSweep) * (clockwise ? 1d : -1d);

        var centerX = ((double)_current.X + end.X) / 2d + cos * cx * rx - sin * cy * ry;

        var centerY = ((double)_current.Y + end.Y) / 2d + sin * cx * rx + cos * cy * ry;
        return AppendArc(new Ellipse(centerX, centerY, rx, ry, rotation), start, sweep, end);
    }

    private VectorPathBuilder AppendArc(Ellipse ellipse, double start, double sweep, Vector2 end, bool full = false)
    {
        var count = (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 2d));
        for (int i = 0; i < count; i++)
        {
            var step = sweep / count;

            var angle = start + step * i;

            var weight = Math.Cos(step / 2d);

            var last = i + 1 == count;
            AddConic(ellipse.Point(angle + step / 2d, 1d / weight),
                last ? end : ellipse.Point(angle + step), (float)weight, smooth: !last || full);
        }
        return this;
    }

    private VectorPathBuilder ConnectTo(Vector2 point) => _current == point
        ? this : Add(new PathCommand(PathVerb.Line, point, Smooth: true));

    private static double SnapQuarterTurn(float angle)
    {
        const double quarter = Math.PI / 2d;

        var nearest = Math.Round(angle / quarter);
        return Math.Abs(nearest) <= 4d && angle == (float)(nearest * quarter) ? nearest * quarter : angle;
    }

    private VectorPathBuilder Add(PathCommand command)
    {
        CheckPoint(command.End);
        CheckPoint(command.Control1);
        CheckPoint(command.Control2);
        if (!_open)
            MoveTo(_current);
        _commands.Add(command);
        _current = command.End;
        return this;
    }

    internal static void CheckTransform(Matrix transform)
    {
        CheckPoint(new Vector2(transform.M11, transform.M12));
        CheckPoint(new Vector2(transform.M21, transform.M22));
        CheckPoint(new Vector2(transform.M41, transform.M42));
        if (transform.M14 != 0f || transform.M24 != 0f || transform.M44 != 1f)
            throw new ArgumentException("路径变换必须为二维仿射矩阵。", nameof(transform));
    }

    private static void CheckPoint(Vector2 point)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            throw new ArgumentOutOfRangeException(nameof(point), "路径坐标必须是有限数值。");
    }

    private static void CheckSize(Vector2 size)
    {
        CheckPoint(size);
        if (size.X < 0f || size.Y < 0f)
            throw new ArgumentOutOfRangeException(nameof(size));
    }

    private readonly record struct Ellipse(double X, double Y, double RadiusX, double RadiusY, double Rotation)
    {
        public Vector2 Point(double angle, double scale = 1d)
        {
            var (sin, cos) = Math.SinCos(angle);
            var (rotationSin, rotationCos) = Math.SinCos(Rotation);
            var x = scale * RadiusX * cos;

            var y = scale * RadiusY * sin;
            return new Vector2((float)(X + x * rotationCos - y * rotationSin),
                (float)(Y + x * rotationSin + y * rotationCos));
        }
    }
}
