using SilkyUIFramework.Graphics2D.Paths;

namespace SilkyUIFramework.Elements;

/// <summary>临时矢量形状组件：统一绘制圆角矩形或圆形，支持实线或虚线描边。</summary>
public sealed class SUITempShape : UIView
{
    private readonly record struct MeshKey(Vector2 Size, float CornerRadius, bool IsCircle,
        float StrokeWidth, float DashLength, float GapLength, Matrix Transform);

    private static readonly PathMesh EmptyMesh = new([], []);

    private MeshKey _meshKey;
    private PathMesh _mesh = EmptyMesh;

    /// <summary>是否绘制为圆形。为 <see langword="false"/> 时绘制圆角矩形。</summary>
    public bool IsCircle { get; set; }

    /// <summary>圆角半径（仅圆角矩形有效）。</summary>
    public float CornerRadius { get; set; } = 8f;

    /// <summary>填充颜色。</summary>
    public Color FillColor { get; set; } = Color.Transparent;

    /// <summary>描边颜色。</summary>
    public Color StrokeColor { get; set; } = Color.Black;

    /// <summary>描边宽度，零表示一个设备像素。</summary>
    public float StrokeWidth { get; set; } = 2f;

    /// <summary>虚线实线段长度，小于等于零时绘制实线。</summary>
    public float DashLength { get; set; }

    /// <summary>虚线间隔长度，小于等于零时与实线段等长。</summary>
    public float GapLength { get; set; }

    internal bool DrawFromParent { get; set; }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        if (!DrawFromParent)
            DrawShape(spriteBatch, InnerBounds.Size, InnerBounds.Position, spriteBatch.transformMatrix);
    }

    internal void DrawShape(SpriteBatch spriteBatch, Vector2 size, Vector2 position, Matrix transform)
    {
        if (size.X <= 0f || size.Y <= 0f || StrokeColor.A == 0) return;

        var origin = SUISvgIcon.PixelOrigin(position, spriteBatch.transformMatrix);
        transform.Translation = Vector3.Zero;
        var key = new MeshKey(size, CornerRadius, IsCircle, StrokeWidth, DashLength, GapLength, transform);
        if (_meshKey != key)
        {
            var builder = new VectorPathBuilder();
            if (IsCircle)
            {
                var radius = MathF.Min(size.X, size.Y) * 0.5f;
                builder.AddCircle(size * 0.5f, radius);
            }
            else
            {
                builder.AddRoundRect(Vector2.Zero, size, new Vector2(CornerRadius));
            }

            var path = builder.Build();
            var stroke = DashLength > 0f
                ? new PathStroke(StrokeWidth, PathLineCap.Round, PathLineJoin.Round,
                    dashes: [DashLength, GapLength > 0f ? GapLength : DashLength])
                : new PathStroke(StrokeWidth, PathLineCap.Round, PathLineJoin.Round);

            // 描边以路径为中心，裁剪范围要包含外侧的半个线宽。
            var width = Vector2.TransformNormal(new Vector2(size.X, 0f), transform);
            var height = Vector2.TransformNormal(new Vector2(0f, size.Y), transform);
            var padding = 1f + StrokeWidth * 0.5f * MathF.Max(
                MathF.Abs(transform.M11) + MathF.Abs(transform.M21),
                MathF.Abs(transform.M12) + MathF.Abs(transform.M22));
            var min = Vector2.Min(Vector2.Zero, width) + Vector2.Min(Vector2.Zero, height) - new Vector2(padding);
            var max = Vector2.Max(Vector2.Zero, width) + Vector2.Max(Vector2.Zero, height) + new Vector2(padding);
            var left = (int)MathF.Floor(min.X);
            var top = (int)MathF.Floor(min.Y);
            var bounds = new Rectangle(left, top, (int)MathF.Ceiling(max.X) - left, (int)MathF.Ceiling(max.Y) - top);
            _mesh = PathRenderer.PrepareStroke(path, stroke, transform, bounds);
            _meshKey = key;
        }

        if (_mesh.IsEmpty) return;
        PathRenderer.Instance.DrawInUI(spriteBatch, _mesh, StrokeColor, origin);
    }
}
