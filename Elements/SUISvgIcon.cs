using SilkyUIFramework.Graphics2D.Paths;

namespace SilkyUIFramework.Elements;

/// <summary>在布局区域内居中、等比绘制 SVG 路径或多层矢量图标。</summary>
[XmlElementMapping("SvgIcon")]
public class SUISvgIcon : UIView
{
    private static readonly PathMesh EmptyMesh = new([], []);
    private VectorPath _path;
    private SvgIcon _icon;
    private PathFillRule _fillRule;
    private Vector4 _viewBox = new(0f, 0f, 24f, 24f);
    private PathMesh _mesh;
    private MeshKey _meshKey;

    /// <summary>SVG 的 path d 数据。赋空值可清除内联路径。</summary>
    public string PathData
    {
        get;
        set
        {
            if (field == value) return;
            _path = string.IsNullOrWhiteSpace(value) ? null : VectorPath.ParseSvgPathData(value, _fillRule);
            field = value;
        }
    }

    /// <summary>内联路径的视框，顺序为 X、Y、宽度和高度。</summary>
    public Vector4 ViewBox
    {
        get => _viewBox;
        set
        {
            SvgIcon.ValidateViewBox(value);
            if (_viewBox == value) return;
            _viewBox = value;
            InvalidateIntrinsicSize();
        }
    }

    /// <summary>内联路径的填充规则，默认为非零环绕。</summary>
    public PathFillRule FillRule
    {
        get => _fillRule;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_fillRule == value) return;
            _path = _path is null ? null : VectorPath.ParseSvgPathData(PathData, value);
            _fillRule = value;
        }
    }

    /// <summary>内联路径的填充颜色。透明色隐藏填充。</summary>
    public Color FillColor { get; set; } = Color.Black;

    /// <summary>内联路径的描边颜色。透明色隐藏描边。</summary>
    public Color StrokeColor { get; set; } = Color.Transparent;

    /// <summary>内联路径的描边宽度，使用视框坐标。零表示一个设备像素。</summary>
    public float StrokeWidth
    {
        get;
        set
        {
            if (!float.IsFinite(value) || value < 0f) throw new ArgumentOutOfRangeException(nameof(value));
            field = value;
        }
    } = 1f;

    /// <summary>内联路径描边的端帽。</summary>
    public PathLineCap StrokeLineCap { get; set; }

    /// <summary>内联路径描边的转角连接方式。</summary>
    public PathLineJoin StrokeLineJoin { get; set; }

    /// <summary>可共享的多层图标。设置后优先使用该图标及其视框。</summary>
    public SvgIcon Icon
    {
        get => _icon;
        set
        {
            if (_icon == value) return;
            _icon = value;
            _mesh = null;
            _meshKey = default;
            InvalidateIntrinsicSize();
        }
    }

    internal bool DrawFromParent { get; set; }

    /// <summary>创建默认尺寸为 24 × 24 的图标控件。</summary>
    public SUISvgIcon() => SetSize(24f, 24f);

    /// <inheritdoc/>
    public override void Measure(float width, float height)
    {
        base.Measure(width, height);
        var viewBox = Icon?.ViewBox ?? ViewBox;
        if (FitWidth) SetInnerBoundsWidthRaw(viewBox.Z);
        if (FitHeightToContent) SetInnerBoundsHeightRaw(viewBox.W);
        ApplyAspectRatioHeight();
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        if (DrawFromParent) return;

        var transform = spriteBatch.transformMatrix;
        var mesh = PrepareMesh(InnerBounds.Size, transform);
        if (mesh.IsEmpty) return;
        var origin = PixelOrigin(InnerBounds.Position, transform);
        var resources = SilkyUISystem.ServiceProvider.GetRequiredService<PathDrawingResources>();
        resources.GetRenderer(spriteBatch.GraphicsDevice).DrawInUI(spriteBatch, mesh, Color.White, origin);
    }

    internal static Point PixelOrigin(Vector2 position, Matrix transform)
    {
        var pixel = Vector2.Transform(position, transform);
        return new Point((int)MathF.Round(pixel.X), (int)MathF.Round(pixel.Y));
    }

    internal PathMesh PrepareMesh(Vector2 size, Matrix transform)
    {
        if (size.X <= 0f || size.Y <= 0f || (Icon is null && _path is null)) return EmptyMesh;
        var linear = LinearPart(transform);
        var key = Icon is { } artwork
            ? new MeshKey(artwork, null, default, default, default, 0f, default, default, size, linear)
            : new MeshKey(null, _path, ViewBox, FillColor, StrokeColor, StrokeWidth, StrokeLineCap, StrokeLineJoin, size, linear);
        if (_mesh is not null && _meshKey == key) return _mesh;

        if (Icon is { } icon)
        {
            _mesh = CompileLayers(icon.ViewBox, icon.Layers, size, linear);
        }
        else
        {
            var fill = new SvgIconLayer(_path, FillColor);
            SvgIconLayer[] layers = StrokeColor.A == 0
                ? [fill]
                : [fill, new SvgIconLayer(_path, StrokeColor, new PathStroke(StrokeWidth, StrokeLineCap, StrokeLineJoin))];
            _mesh = CompileLayers(ViewBox, layers, size, linear);
        }
        _meshKey = key;
        return _mesh;
    }

    internal PathMesh PrepareLayerRange(int start, int count, Vector2 size, Matrix transform, Matrix pose) =>
        CompileLayers(Icon.ViewBox, Icon.Layers.Slice(start, count), size, LinearPart(transform), pose);

    private static Matrix LinearPart(Matrix transform) =>
        new(transform.M11, transform.M12, 0f, 0f, transform.M21, transform.M22, 0f, 0f,
            0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);

    private static PathMesh CompileLayers(Vector4 viewBox, ReadOnlySpan<SvgIconLayer> layers, Vector2 size, Matrix linear,
        Matrix? pose = null)
    {
        if (linear.M11 * linear.M22 - linear.M12 * linear.M21 == 0f) return EmptyMesh;
        var viewSize = new Vector2(viewBox.Z, viewBox.W);
        var fit = Math.Min(size.X / viewSize.X, size.Y / viewSize.Y);
        if (!(fit > 0f) || !float.IsFinite(fit)) return EmptyMesh;
        var topLeft = (size - viewSize * fit) / 2f - new Vector2(viewBox.X, viewBox.Y) * fit;
        var pathTransform = Matrix.CreateScale(fit, fit, 1f) * Matrix.CreateTranslation(topLeft.X, topLeft.Y, 0f) * linear;
        if (pose is Matrix localPose) pathTransform = localPose * pathTransform;
        var width = Vector2.TransformNormal(new Vector2(size.X, 0f), linear);
        var height = Vector2.TransformNormal(new Vector2(0f, size.Y), linear);
        var min = Vector2.Min(Vector2.Min(Vector2.Zero, width), Vector2.Min(height, width + height));
        var max = Vector2.Max(Vector2.Max(Vector2.Zero, width), Vector2.Max(height, width + height));
        var bounds = new Rectangle((int)MathF.Floor(min.X), (int)MathF.Floor(min.Y),
            (int)MathF.Ceiling(max.X) - (int)MathF.Floor(min.X), (int)MathF.Ceiling(max.Y) - (int)MathF.Floor(min.Y));
        var inverse = Matrix.Invert(pathTransform);
        var vertices = new List<VertexPositionColor>();
        var indices = new List<int>();
        foreach (var layer in layers)
        {
            var end = layer.BottomColor ?? layer.Color;
            if (layer.Color.A == 0 && end.A == 0) continue;
            var mesh = layer.Stroke is { } stroke
                ? PathRenderer.PrepareStroke(layer.Path, stroke, pathTransform, bounds)
                : PathRenderer.PrepareFill(layer.Path, pathTransform, bounds);
            var startIndex = vertices.Count;
            foreach (var vertex in mesh.Vertices)
            {
                var color = layer.Color;
                if (end != color)
                {
                    var localY = Vector2.Transform(new Vector2(vertex.Position.X, vertex.Position.Y), inverse).Y;
                    color = Color.Lerp(color, end, Math.Clamp((localY - viewBox.Y) / viewBox.W, 0f, 1f));
                }
                vertices.Add(new VertexPositionColor(vertex.Position, color * (vertex.Color.A / 255f)));
            }
            foreach (var index in mesh.Indices) indices.Add(index + startIndex);
        }
        return new PathMesh(vertices.ToArray(), indices.ToArray(), pathTransform);
    }

    private void InvalidateIntrinsicSize()
    {
        if (FitWidth || FitHeight) MarkLayoutDirty();
    }

    private readonly record struct MeshKey(SvgIcon Icon, VectorPath Path, Vector4 ViewBox,
        Color Fill, Color Stroke, float StrokeWidth, PathLineCap Cap, PathLineJoin Join, Vector2 Size, Matrix Linear);
}
