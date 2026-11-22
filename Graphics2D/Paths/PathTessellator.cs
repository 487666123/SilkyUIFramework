namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>已经按目标像素尺寸准备好的路径网格，可换颜色后重复绘制。</summary>
public sealed class PathMesh
{
    private readonly int _vertexCount;
    private readonly int _indexCount;

    internal VertexPositionColor[] VertexBuffer { get; }
    internal int[] IndexBuffer { get; }
    internal ReadOnlySpan<VertexPositionColor> Vertices => VertexBuffer.AsSpan(0, _vertexCount);
    internal ReadOnlySpan<int> Indices => IndexBuffer.AsSpan(0, _indexCount);
    internal Matrix LocalToPixel { get; }

    /// <summary>获取网格是否没有可绘制的三角形。</summary>
    public bool IsEmpty => _indexCount == 0;

    internal PathMesh(VertexPositionColor[] vertices, int[] indices) : this(vertices, indices, Matrix.Identity) { }

    internal PathMesh(VertexPositionColor[] vertices, int[] indices, Matrix localToPixel)
        : this(vertices, vertices.Length, indices, indices.Length, localToPixel) { }

    internal PathMesh(VertexPositionColor[] vertices, int vertexCount, int[] indices, int indexCount, Matrix localToPixel)
    {
        VertexBuffer = vertices;
        IndexBuffer = indices;
        _vertexCount = vertexCount;
        _indexCount = indexCount;
        LocalToPixel = localToPixel;
    }
}

internal sealed class PathTessellator(Rectangle bounds, PathFillRule rule, bool antialias)
{
    private readonly Rectangle _bounds = bounds;

    private readonly bool _evenOdd = rule is PathFillRule.EvenOdd or PathFillRule.InverseEvenOdd;

    private readonly bool _inverse = rule is PathFillRule.InverseNonZero or PathFillRule.InverseEvenOdd;

    private readonly bool _antialias = antialias;

    private readonly List<Edge> _edges = [];

    private readonly List<VertexPositionColor> _vertices = [];

    private readonly List<int> _indices = [];

    private readonly SortedDictionary<int, double> _coverageChanges = [];

    private int? _coverageRow;

    public void AddContour(IEnumerable<Vector2> contour, Matrix transform)
    {
        var first = default(Vector2);

        var previous = default(Vector2);

        var hasPoint = false;
        foreach (var point in contour)
        {
            var current = Vector2.Transform(point, transform);
            if (!float.IsFinite(current.X) || !float.IsFinite(current.Y))
                throw new ArgumentException("路径变换后的坐标必须是有限数值。", nameof(transform));
            if (hasPoint)
                AddEdge(previous, current);
            else
                first = current;
            hasPoint = true;
            previous = current;
        }
        if (hasPoint)
            AddEdge(previous, first);
    }

    public PathMesh Build(Matrix localToPixel)
    {
        if (_edges.Count == 0)
        {
            if (_inverse)
                AddQuad(_bounds.Left, _bounds.Right, _bounds.Left, _bounds.Right, _bounds.Top, _bounds.Bottom);
            return new PathMesh(_vertices.ToArray(), _indices.ToArray(), localToPixel);
        }

        _edges.Sort((a, b) => a.Top.Y.CompareTo(b.Top.Y));
        var events = FindEvents();

        var active = new List<Edge>();

        var crossings = new List<(Edge Edge, double X)>();

        var spans = new List<(Edge Left, Edge Right)>();

        var clipLeft = new Edge(new Vector2(_bounds.Left, _bounds.Top), new Vector2(_bounds.Left, _bounds.Bottom), 0);

        var clipRight = new Edge(new Vector2(_bounds.Right, _bounds.Top), new Vector2(_bounds.Right, _bounds.Bottom), 0);

        var next = 0;
        for (int i = 0; i + 1 < events.Length; i++)
        {
            var top = events[i];

            var bottom = events[i + 1];

            var middle = top + (bottom - top) * 0.5d;
            active.RemoveAll(edge => edge.Bottom.Y <= top);
            while (next < _edges.Count && _edges[next].Top.Y <= top)
            {
                if (_edges[next].Bottom.Y > top)
                    active.Add(_edges[next]);
                next++;
            }
            crossings.Clear();
            foreach (var edge in active)
                crossings.Add((edge, edge.XAt(middle)));
            crossings.Sort((a, b) => a.X.CompareTo(b.X));
            spans.Clear();
            var winding = 0;

            var start = clipLeft;
            foreach (var (edge, _) in crossings)
            {
                var wasInside = IsInside(winding);
                winding += edge.Winding;
                if (wasInside == IsInside(winding))
                    continue;
                if (wasInside)
                    spans.Add((start, edge));
                else
                    start = edge;
            }
            if (IsInside(winding))
                spans.Add((start, clipRight));

            if (_antialias)
                AccumulateCoverage(top, bottom, spans);
            else
            {
                foreach (var (left, right) in spans)
                    AddQuad(ClampedX(left, top), ClampedX(right, top), ClampedX(left, bottom), ClampedX(right, bottom), top, bottom);
            }
        }
        FlushCoverage();
        return new PathMesh(_vertices.ToArray(), _indices.ToArray(), localToPixel);
    }

    private void AddEdge(Vector2 from, Vector2 to)
    {
        if (from.Y == to.Y) return;
        var edge = from.Y < to.Y ? new Edge(from, to, 1) : new Edge(to, from, -1);
        if (edge.Bottom.Y > _bounds.Top && edge.Top.Y < _bounds.Bottom)
            _edges.Add(edge);
    }

    // 顶点、交点和裁剪边将路径分成水平带。每条带内，边的相对顺序固定。
    private double[] FindEvents()
    {
        var events = new SortedSet<double> { _bounds.Top, _bounds.Bottom };

        var active = new List<Edge>();
        foreach (var edge in _edges)
        {
            var top = Math.Max((double)edge.Top.Y, _bounds.Top);

            var bottom = Math.Min((double)edge.Bottom.Y, _bounds.Bottom);
            events.Add(top);
            events.Add(bottom);
            active.RemoveAll(previous => previous.Bottom.Y <= top);
            foreach (var previous in active)
            {
                var end = Math.Min(bottom, previous.Bottom.Y);
                AddCrossing(events, top, end, edge.XAt(top) - previous.XAt(top), edge.XAt(end) - previous.XAt(end));
            }
            var x0 = edge.XAt(top);

            var x1 = edge.XAt(bottom);
            AddCrossing(events, top, bottom, x0 - _bounds.Left, x1 - _bounds.Left);
            AddCrossing(events, top, bottom, x0 - _bounds.Right, x1 - _bounds.Right);
            active.Add(edge);
        }
        return events.ToArray();
    }

    private static void AddCrossing(SortedSet<double> events, double top, double bottom, double a, double b)
    {
        if ((a < 0d && b > 0d) || (a > 0d && b < 0d))
        {
            var y = top + (bottom - top) * (a / (a - b));
            if (y > top && y < bottom)
                events.Add(y);
        }
    }

    private bool IsInside(int winding) => (_evenOdd ? (winding & 1) != 0 : winding != 0) != _inverse;

    private double ClampedX(Edge edge, double y) => Math.Clamp(edge.XAt(y), _bounds.Left, _bounds.Right);

    // 横向按区间与像素的交长计算，纵向用每像素四个采样区间估算面积。
    // 每个像素只输出一次，半透明的描边和自交区域不会叠加多次。
    private void AccumulateCoverage(double top, double bottom, List<(Edge Left, Edge Right)> spans)
    {
        if (spans.Count == 0) return;
        for (var y = top; y < bottom;)
        {
            var row = (int)Math.Floor(y);
            if (_coverageRow != row)
            {
                FlushCoverage();
                _coverageRow = row;
            }
            var end = Math.Min(bottom, (Math.Floor(y * 4d) + 1d) * 0.25d);

            var middle = y + (end - y) * 0.5d;
            foreach (var (left, right) in spans)
                AccumulateSpan(ClampedX(left, middle), ClampedX(right, middle), end - y);
            y = end;
        }
    }

    private void AccumulateSpan(double left, double right, double height)
    {
        if (right <= left) return;
        var first = (int)Math.Floor(left);

        var last = (int)Math.Ceiling(right) - 1;
        if (first == last)
        {
            AddCoverage(first, first + 1, (right - left) * height);
            return;
        }
        AddCoverage(first, first + 1, (first + 1d - left) * height);
        AddCoverage(first + 1, last, height);
        AddCoverage(last, last + 1, (right - last) * height);
    }

    private void AddCoverage(int start, int end, double amount)
    {
        if (start >= end) return;
        _coverageChanges[start] = _coverageChanges.GetValueOrDefault(start) + amount;
        _coverageChanges[end] = _coverageChanges.GetValueOrDefault(end) - amount;
    }

    private void FlushCoverage()
    {
        if (_coverageRow is not int row) return;
        double coverage = 0d;

        int start = 0;

        byte alpha = 0;
        foreach (var (x, change) in _coverageChanges)
        {
            coverage += change;
            var next = (byte)Math.Round(255d * Math.Clamp(coverage, 0d, 1d));
            if (next == alpha) continue;
            if (alpha != 0)
                AddQuad(start, x, start, x, row, row + 1d, alpha);
            start = x;
            alpha = next;
        }
        _coverageChanges.Clear();
    }

    private void AddQuad(double leftTop, double rightTop, double leftBottom, double rightBottom,
        double top, double bottom, byte alpha = 255)
    {
        if (rightTop <= leftTop && rightBottom <= leftBottom) return;
        var left0 = (float)leftTop;

        var right0 = (float)Math.Max(leftTop, rightTop);

        var left1 = (float)leftBottom;

        var right1 = (float)Math.Max(leftBottom, rightBottom);
        if ((float)top == (float)bottom || (left0 == right0 && left1 == right1)) return;
        var color = new Color(alpha, alpha, alpha, alpha);

        var first = _vertices.Count;
        _vertices.Add(new VertexPositionColor(new Vector3(left0, (float)top, 0f), color));
        _vertices.Add(new VertexPositionColor(new Vector3(right0, (float)top, 0f), color));
        _vertices.Add(new VertexPositionColor(new Vector3(left1, (float)bottom, 0f), color));
        _vertices.Add(new VertexPositionColor(new Vector3(right1, (float)bottom, 0f), color));
        _indices.Add(first);
        _indices.Add(first + 1);
        _indices.Add(first + 2);
        _indices.Add(first + 2);
        _indices.Add(first + 1);
        _indices.Add(first + 3);
    }

    private readonly record struct Edge(Vector2 Top, Vector2 Bottom, int Winding)
    {
        public double XAt(double y) => Top.X + (y - Top.Y) / ((double)Bottom.Y - Top.Y) * ((double)Bottom.X - Top.X);
    }
}
