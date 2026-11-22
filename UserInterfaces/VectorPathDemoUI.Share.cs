using SilkyUIFramework.Graphics2D.Paths;

namespace SilkyUIFramework.UserInterfaces;

public sealed partial class VectorPathDemoUI
{
    private const float ShareOuterEdgeWidth = 1.4f;
    private const float ShareInnerEdgeWidth = 1f;
    // 渐变填充盖住描边的内半部，露出的宽度由分享组件独立定义。
    private static readonly PathStroke ShareEdgeStroke = new(
        2f * (ShareOuterEdgeWidth + ShareInnerEdgeWidth), PathLineCap.Round, PathLineJoin.Round);
    private static readonly PathStroke ShareInnerEdgeStroke = new(
        2f * ShareInnerEdgeWidth, PathLineCap.Round, PathLineJoin.Round);
    private static readonly VectorPath ShareButtonOutline = new VectorPathBuilder().AddCircle(Vector2.Zero, ShareButtonRadius).Build();
    private const float ShareButtonRadius = 16f;
    private const float ShareBubbleLift = 33f;
    private const float ShareBubbleExtraLift = 11f;
    private const float ShareBubbleHalfHeight = 10f;
    private const float SharePinchGap = 1.8f;
    private const float ShareDetachGap = 4.2f;

    private readonly ShareButtonView _shareButton = new();
    private readonly ShareTooltipView _shareTooltip = new() { IgnoreMouseInteraction = true };
    private Matrix _shareTransform;
    private ShareSurface _shareButtonSurface;
    private ShareSurface _shareSurface;
    private float _shareProgress;
    private float _shareMeshProgress;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        base.OnInitialize();
        AddChild(_shareButton);
        AddChild(_shareTooltip);
    }

    private void PrepareShareDemo(Matrix transform, Rectangle bounds)
    {
        var center = _layout.ShareButtonCenter;
        _shareTransform = Matrix.CreateScale(_layout.Scale, _layout.Scale, 1f) *
            Matrix.CreateTranslation(center.X, center.Y, 0f) * transform;
        _shareButton.Prepare(_shareTransform, bounds);
        _shareButtonSurface = new ShareSurface(ShareButtonOutline, _shareTransform, bounds, -ShareButtonRadius);
        _shareSurface = null;
    }

    private void DrawShareDemo(GameTime gameTime, SpriteBatch spriteBatch, PathRenderer renderer,
        GalleryLayout layout, Point offset)
    {
        _shareButton.IsShown = true;
        _shareButton.Center = layout.ShareButtonCenter;
        _shareButton.Radius = ShareButtonRadius * layout.Scale;
        _shareButton.PixelOffset = offset;

        var hovering = Main.instance.IsActive && !_dragGrabOffset.HasValue && _shareButton.IsMouseHovering;
        var step = (float)gameTime.ElapsedGameTime.TotalSeconds / (hovering ? 1.3f : 0.76f);
        _shareProgress = Math.Clamp(_shareProgress + (hovering ? step : -step), 0f, 1f);

        var eased = MathHelper.SmoothStep(0f, 1f, _shareProgress);
        // 在细颈阶段连续减速。速度只在起点和终点降到零。
        var motion = eased + 0.78f * MathF.Sin(MathHelper.TwoPi * eased) / MathHelper.TwoPi;
        // 把减速段放在两侧轮廓已拉开、细颈还较宽的位置。
        motion += 0.74f * motion * (1f - motion);
        // 细颈分离后，让提示框继续上移，为两侧描边留出间距。
        var lift = MathHelper.Lerp(10f, ShareBubbleLift, motion) + ShareBubbleExtraLift * SharePhase(motion, 0.88f, 1f);
        var bubbleCenter = new Vector2(0f, -lift);
        var growth = SharePhase(motion, 0f, 0.5f);
        var bubbleHalfSize = Vector2.Lerp(new Vector2(7f, 5f), new Vector2(24f, ShareBubbleHalfHeight), growth);
        _shareTooltip.Center = _shareButton.Center + bubbleCenter * layout.Scale;
        _shareTooltip.TextScale = 0.72f * layout.Scale;
        _shareTooltip.Opacity = SharePhase(motion, 0.33f, 0.62f);

        if (_shareSurface is null || _shareMeshProgress != _shareProgress)
        {
            var outline = CreateShareOutline(bubbleCenter, bubbleHalfSize, _shareProgress > 0f);
            // 两个轮廓在分离后仍使用同一套渐变坐标。
            _shareSurface = ReferenceEquals(outline, ShareButtonOutline) ? _shareButtonSurface :
                new ShareSurface(outline, _shareTransform, _meshBounds,
                    Math.Min(-ShareButtonRadius, bubbleCenter.Y - bubbleHalfSize.Y));
            _shareMeshProgress = _shareProgress;
        }
        _shareSurface.Draw(spriteBatch, renderer, offset);
    }

    private static PathGradient CreateShareBrush(float top, Color light, Color dark) =>
        PathGradient.Linear(new Vector2(0f, top), new Vector2(0f, ShareButtonRadius), [light, dark]);

    private static float SharePhase(float progress, float start, float end) =>
        MathHelper.SmoothStep(0f, 1f, Math.Clamp((progress - start) / (end - start), 0f, 1f));

    private static VectorPath CreateShareOutline(Vector2 bubbleCenter, Vector2 bubbleHalfSize, bool showBubble)
    {
        if (!showBubble) return ShareButtonOutline;

        var outline = new VectorPathBuilder();
        var buttonRadii = new Vector2(ShareButtonRadius);

        var lift = -bubbleCenter.Y;
        var emerge = SharePhase(lift, 10f, 26f);
        var gap = -ShareButtonRadius - (bubbleCenter.Y + bubbleHalfSize.Y);
        var pinch = SharePhase(gap, SharePinchGap, ShareDetachGap);
        var detached = gap >= ShareDetachGap;
        var joinAngle = 0.75f * emerge * (1f - 0.75f * pinch);
        var buttonRight = buttonRadii * new Vector2(MathF.Sin(joinAngle), -MathF.Cos(joinAngle));
        var bubbleTop = Math.Min(bubbleCenter.Y - bubbleHalfSize.Y, -ShareButtonRadius);
        var bubbleBottom = Math.Min(bubbleCenter.Y + bubbleHalfSize.Y, buttonRight.Y - 2f * emerge);
        var halfWidth = bubbleHalfSize.X * emerge;
        if (bubbleBottom <= bubbleTop || halfWidth <= 0f)
            return ShareButtonOutline;

        var corner = Math.Min(8f, Math.Min((bubbleBottom - bubbleTop) * 0.5f, halfWidth * 0.75f));
        var cornerRadii = new Vector2(corner);
        var root = Math.Min(12f, halfWidth - corner);
        var rootRight = new Vector2(root, bubbleBottom);
        var rootLeft = Mirror(rootRight);
        var waistY = MathHelper.Lerp((bubbleBottom + buttonRight.Y) * 0.5f, bubbleBottom + 2.7f, pinch);
        var waistX = Math.Min(root, buttonRight.X) * 0.65f * (1f - pinch);
        var waist = new Vector2(waistX, waistY);
        var upperHandle = Math.Max(0f, waistY - bubbleBottom) * 0.45f * (1f - pinch);
        var upperControl = new Vector2(waistX + 3f * pinch, waistY - upperHandle);
        var rootControl = new Vector2(root * 0.72f, bubbleBottom);

        // 从提示左侧沿外边缘画到右侧，底边中央留给细颈或圆尖。
        outline.MoveTo(rootLeft)
            .ArcTo(new Vector2(-halfWidth + corner, bubbleBottom - corner), cornerRadii, MathHelper.PiOver2, MathHelper.PiOver2)
            .ArcTo(new Vector2(-halfWidth + corner, bubbleTop + corner), cornerRadii, MathHelper.Pi, MathHelper.PiOver2)
            .ArcTo(new Vector2(halfWidth - corner, bubbleTop + corner), cornerRadii, -MathHelper.PiOver2, MathHelper.PiOver2)
            .ArcTo(new Vector2(halfWidth - corner, bubbleBottom - corner), cornerRadii, 0f, MathHelper.PiOver2)
            .LineTo(rootRight)
            .CubicTo(rootControl, upperControl, waist);

        if (!detached)
        {
            // 细颈与按钮共享圆弧端点，控制点沿圆弧切线排列。
            var circleTangent = new Vector2(MathF.Cos(joinAngle), MathF.Sin(joinAngle));
            var lowerHeight = buttonRight.Y - waistY;
            // 腰部两侧的曲线沿同一条切线，收细时也保持圆滑。
            var neckTangent = waist - upperControl;
            var handleScale = Math.Min(1f, waistX / Math.Max(0.001f, -2f * neckTangent.X));
            var lowerControl = waist + neckTangent * handleScale;
            var buttonControl = buttonRight - circleTangent * Math.Min(lowerHeight * 0.45f, buttonRight.X * 0.55f);
            outline.CubicTo(lowerControl, buttonControl, buttonRight)
                .ArcTo(Vector2.Zero, buttonRadii, -MathHelper.PiOver2 + joinAngle, MathHelper.TwoPi - 2f * joinAngle)
                .CubicTo(Mirror(buttonControl), Mirror(lowerControl), Mirror(waist));
        }
        outline.CubicTo(Mirror(upperControl), Mirror(rootControl), rootLeft).Close();
        if (detached) outline.AddPath(ShareButtonOutline);
        return outline.Build();

        static Vector2 Mirror(Vector2 point) => new(-point.X, point.Y);
    }

    private sealed class ShareSurface(VectorPath outline, Matrix transform, Rectangle bounds, float gradientTop)
    {
        private static readonly Color EdgeLight = Color.Lerp(VectorPathDemoIcons.Blue.Edge, VectorPathDemoIcons.Blue.Light, 0.48f);
        private readonly PathGradient _fillBrush = CreateShareBrush(gradientTop, VectorPathDemoIcons.Blue.Light, VectorPathDemoIcons.Blue.Dark);
        // 外框与底色共用渐变坐标，让浅色区域的外框也相应提亮。
        private readonly PathGradient _edgeBrush = CreateShareBrush(gradientTop, EdgeLight, VectorPathDemoIcons.Blue.Edge);
        private readonly PathMesh _face = PathRenderer.PrepareFill(outline, transform, bounds);
        private readonly PathMesh _edge = PathRenderer.PrepareStroke(outline, ShareEdgeStroke, transform, bounds);
        private readonly PathMesh _highlight = PathRenderer.PrepareStroke(outline, ShareInnerEdgeStroke, transform, bounds);
        private readonly int _shadowOffset = Math.Max(1, (int)MathF.Round(1.5f * transform.M22));

        public void Draw(SpriteBatch spriteBatch, PathRenderer renderer, Point offset)
        {
            var palette = VectorPathDemoIcons.Blue;
            renderer.DrawInUI(spriteBatch, _face, palette.Edge * 0.16f, new Point(offset.X, offset.Y + _shadowOffset));
            renderer.DrawGradientInUI(spriteBatch, _edge, _edgeBrush, offset);
            // 用与图标一致的白色高光强度混合描边，渐变覆盖描边内半部。
            renderer.DrawInUI(spriteBatch, _highlight, Color.White * 0.45f, offset);
            renderer.DrawGradientInUI(spriteBatch, _face, _fillBrush, offset);
        }
    }

    private sealed class ShareButtonView : UIView
    {
        private static readonly PathStroke GlyphStroke = new(2f, PathLineCap.Round, PathLineJoin.Round);
        private static readonly VectorPath Glyph = new VectorPathBuilder()
            .MoveTo(new Vector2(-2.5f, -1f)).LineTo(new Vector2(2.5f, -4f))
            .MoveTo(new Vector2(-2.5f, 1f)).LineTo(new Vector2(2.5f, 4f))
            .AddCircle(new Vector2(-4f, 0f), 1.7f)
            .AddCircle(new Vector2(4f, -5f), 1.7f)
            .AddCircle(new Vector2(4f, 5f), 1.7f)
            .Build();

        private PathMesh _glyphMesh;

        public bool IsShown { get; set; }
        public Vector2 Center { get; set; }
        public float Radius { get; set; }
        public Point PixelOffset { get; set; }

        public override bool ContainsPoint(Vector2 point) =>
            IsShown && Vector2.DistanceSquared(point, Center) <= Radius * Radius;

        public void Prepare(Matrix transform, Rectangle bounds) =>
            _glyphMesh = PathRenderer.PrepareStroke(Glyph, GlyphStroke, transform, bounds);

        protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsShown) return;
            var resources = SilkyUISystem.ServiceProvider.GetRequiredService<VectorPathDemoResources>();
            resources.GetRenderer(spriteBatch.GraphicsDevice).DrawInUI(spriteBatch, _glyphMesh, Color.White, PixelOffset);
        }
    }

    private sealed class ShareTooltipView : UIView
    {
        private const string Label = "分享";
        public Vector2 Center { get; set; }
        public float TextScale { get; set; }
        public float Opacity { get; set; }

        protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (Opacity <= 0f) return;
            var font = FontAssets.MouseText.Value;
            var origin = font.MeasureString(Label) * 0.5f;
            var position = Center + new Vector2(0f, TextDrawingHelper.GetFontOffset(font) * TextScale);
            spriteBatch.DrawString(font, Label, position, new Color(28, 62, 115) * Opacity, 0f, origin, TextScale, 0, 0f);
        }
    }
}
