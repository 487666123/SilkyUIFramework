using SilkyUIFramework.Graphics2D.Paths;

namespace SilkyUIFramework.UserInterfaces;

/// <summary>在游戏内展示路径图标及线性和径向渐变。</summary>
[RegisterUI]
public sealed partial class VectorPathDemoUI : BaseBody
{
    private static readonly Color PanelColor = Color.White;

    private static readonly Color OutlineColor = new(222, 226, 234);

    private static readonly Color TileColor = new(246, 247, 250);

    private static readonly Color SelectedColor = new(231, 239, 255);

    private static readonly Color AccentColor = new(56, 107, 212);

    private static readonly Color TextColor = new(37, 44, 58);

    private static readonly Color HintColor = new(97, 106, 124);

    private static readonly Vector2 LauncherSize = new(132f, 42f);

    private const float LauncherDragThreshold = 7f;

    private const float CategorySlideSeconds = 0.18f;

    private const float CategoryLabelInset = 34f;

    private static readonly PathStroke LauncherChevronStroke = new(1.8f, PathLineCap.Round, PathLineJoin.Round);

    private static readonly PathStroke CategoryGlyphStroke = new(1.6f, PathLineCap.Round, PathLineJoin.Round);

    private static readonly VectorPath LauncherGlyph = VectorPath.ParseSvgPathData(
        "M13 15.5H17V19.5H13Z M20 15.5H24V19.5H20Z M13 22.5H17V26.5H13Z M20 22.5H24V26.5H20Z");

    private static readonly VectorPath LauncherChevron = VectorPath.ParseSvgPathData("M111 16L116 21L111 26");

    private static readonly VectorPath LinearSample = new VectorPathBuilder()
        .AddRoundRect(Vector2.Zero, new Vector2(114f, 29f), new Vector2(8f)).Build();

    private static readonly VectorPath RadialSample = new VectorPathBuilder()
        .AddCircle(new Vector2(22f), 22f).Build();

    private static readonly PathGradient LinearBrush = PathGradient.Linear(
        Vector2.Zero, new Vector2(114f, 29f),
        [new Color(57, 116, 238), new Color(126, 87, 226), new Color(228, 99, 157)],
        [0f, 0.42f, 1f]);

    private static readonly PathGradient RadialBrush = PathGradient.Radial(
        new Vector2(16f, 14f), 29f, [new Color(220, 255, 224), new Color(20, 144, 119)]);

    private static readonly Category[] Categories =
    [
        new("全部图标", "M2 2H7V7H2Z M11 2H16V7H11Z M2 11H7V16H2Z M11 11H16V16H11Z"),
        new("图表", "M2 3V16H16 M6 12V9 M10 12V5 M14 12V7",
            "数据看板", "折线图", "图表", "饼图", "环形图", "面积图", "漏斗图", "动态", "上涨", "下跌", "仪表", "热力图"),
        new("界面与布局", "M2 3H16V15H2Z M2 7H16 M7 7V15", "看板", "侧边栏", "列表视图", "网格"),
        new("文件与数据", "M4 2H10L14 6V16H4Z M10 2V6H14 M7 10H11 M7 13H11",
            "收件箱", "数据库", "服务器", "报表文件", "演示", "下载", "导出"),
        new("人物与通知", "M12 5A3 3 0 1 0 6 5A3 3 0 1 0 12 5Z M3 16V15Q3 11 7 11H11Q15 11 15 15V16",
            "多人", "用户", "通知", "日历"),
        new("操作与状态", "M2 5H6 M10 5H16 M6 3H10V7H6Z M2 13H9 M13 13H16 M9 11H13V15H9Z",
            "设置", "调节", "筛选", "搜索", "同步", "完成", "警告", "更多",
            "全部拿走", "全部放入", "快速堆叠", "排序"),
    ];

    private const int MinIconSize = 24;
    private const int MaxIconSize = 48;

    private IReadOnlyList<VectorPathDemoIcons.Icon> _visibleIcons = VectorPathDemoIcons.All;

    private int _category;

    private float _categorySlideStart;

    private float _categorySlidePosition;

    private float _categorySlideElapsed = CategorySlideSeconds;

    private PathMesh _categoryHighlight;

    private PathMesh _categoryGlyphs;

    private int _iconSize = 32;
    private bool _sizeDragging;
    private PathMesh _iconGrid;
    private PathMesh _sizeSlider;

    private PathMesh _gallery;

    private const float SyncHalfTurnSeconds = 0.22f;

    private const float SyncPressedScale = 0.82f;

    private PathMesh _syncIcon;

    private PathMesh _syncAnimatedIcon;

    private Matrix _syncTransform;

    private int _syncTile = -1;

    private bool _syncHovered;

    private bool _syncPressed;

    private float _syncAngle;

    private float _syncTurnStart;

    private float _syncTurnElapsed = SyncHalfTurnSeconds;

    private float _syncScale = 1f;

    private PathMesh _cloverGreenMesh;

    private PathMesh _cloverPinkMesh;

    private int _cloverTile = -1;

    private bool _cloverPressed;

    private Matrix _cloverTransform;

    private float _cloverPressStart;

    private float _cloverPressProgress;

    private float _cloverPressElapsed;

    private PathMesh _linearSample;

    private PathMesh _radialSample;

    private GalleryLayout _layout;

    private Matrix _meshTransform;

    private Rectangle _meshBounds;

    private Vector2 _dragOffset;

    private Vector2? _dragGrabOffset;

    private bool _loggedFirstDraw;

    private bool _collapsed;

    private bool _closePressed;

    private float _closeAnimation;
    private PathMesh _closeMesh;
    private readonly Dictionary<float, PathStroke> _closeStrokes = new();

    private Vector2? _launcherPosition;

    private Vector2 _launcherViewport;

    private Vector2 _launcherScale;

    private PathMesh _launcherMesh;

    private (Vector2 Mouse, Vector2 Position)? _launcherPress;

    private bool _launcherDragged;

    /// <inheritdoc/>
    public override bool IsInteractable => !Main.gameMenu;

    /// <inheritdoc/>
    public override UIView GetElementAt(Vector2 mousePosition)
    {
        if (!IsInteractable || _layout.Scale <= 0f || SilkyUI is null) return null;
        var target = _collapsed ? LauncherBox : DisplayLayout().Panel;
        if (!target.Contains(mousePosition)) return null;
        return _collapsed ? this : _shareButton.GetElementAt(mousePosition) ?? this;
    }

    /// <inheritdoc/>
    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
        if (evt.Source != this || !IsInteractable) return;

        var mouse = evt.MousePosition;
        if (_collapsed)
        {
            _launcherPress = (mouse, LauncherBox.Position);
            _launcherDragged = false;
            return;
        }

        var layout = DisplayLayout();
        if (_gallery is not null && _cloverTile >= 0 && layout.Tile(_cloverTile).Contains(mouse))
        {
            SetCloverPressed(true);
            return;
        }
        if (_gallery is not null && _syncTile >= 0 && layout.Tile(_syncTile).Contains(mouse))
        {
            _syncPressed = true;
            return;
        }
        if (layout.CloseButton.Contains(mouse))
        {
            _closePressed = true;
            return;
        }
        if (layout.Header.Contains(mouse))
        {
            _dragGrabOffset = mouse - layout.Origin;
            return;
        }

        for (var i = 0; i < Categories.Length; i++)
        {
            if (layout.Category(i).Contains(mouse))
            {
                if (_category == i) return;
                _categorySlideStart = _categorySlidePosition;
                _categorySlideElapsed = 0f;
                _category = i;
                var names = Categories[i].IconNames;
                _visibleIcons = names.Length == 0 ? VectorPathDemoIcons.All :
                    VectorPathDemoIcons.All.Where(icon => names.Contains(icon.Name)).ToArray();
                _gallery = null;
                return;
            }
        }

        if (layout.SizeSlider.Contains(mouse))
        {
            _sizeDragging = true;
            SetIconSize(layout, mouse.X);
        }
    }

    /// <inheritdoc/>
    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        base.OnLeftMouseUp(evt);
        if (_sizeDragging)
        {
            _sizeDragging = false;
            SetIconSize(DisplayLayout(), evt.MousePosition.X);
        }
        _dragGrabOffset = null;
        SetCloverPressed(false);
        if (_syncPressed)
        {
            _syncPressed = false;
            return;
        }
        if (_closePressed)
        {
            _closePressed = false;
            var closeButton = DisplayLayout().CloseButton;
            if (closeButton.Contains(evt.MousePosition))
            {
                _launcherPosition ??= closeButton.Center - LauncherSize / 2f;
                _collapsed = true;
            }
            return;
        }
        if (!_collapsed || !_launcherPress.HasValue) return;
        MoveLauncher(evt.MousePosition);
        _launcherPress = null;
        if (!_launcherDragged && LauncherBox.Contains(evt.MousePosition))
            _collapsed = false;
    }

    private void SetIconSize(GalleryLayout layout, float mouseX)
    {
        var track = layout.SizeTrack;
        var progress = Math.Clamp((mouseX - track.Position.X) / track.Size.X, 0f, 1f);
        var size = (int)MathF.Round(MathHelper.Lerp(MinIconSize, MaxIconSize, progress));
        if (_iconSize == size) return;
        _iconSize = size;
        _iconGrid = null;
    }

    /// <inheritdoc/>
    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);
        // 鼠标抬起由框架的事件结束手势，绘制阶段不能提前清除按下记录。
        if (!Main.instance.IsActive)
        {
            _dragGrabOffset = null;
            _launcherPress = null;
            _sizeDragging = false;
            _closePressed = false;
            _syncPressed = false;
            SetCloverPressed(false);
        }
        var mouse = new Vector2(Main.mouseX, Main.mouseY);
        if (_sizeDragging) SetIconSize(DisplayLayout(), mouse.X);
        if (IsInteractable && (_dragGrabOffset.HasValue || _launcherPress.HasValue || _sizeDragging ||
            GetElementAt(mouse) is not null))
            Main.LocalPlayer.mouseInterface = true;
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        _shareButton.IsShown = false;
        _shareTooltip.Opacity = 0f;
        if (Main.gameMenu) return;
        var transform = SilkyUI.TransformMatrix;

        var viewport = spriteBatch.GraphicsDevice.Viewport;
        if (transform.M11 <= 0f || transform.M22 <= 0f || viewport.Width <= 0 || viewport.Height <= 0) return;
        var viewportSize = new Vector2(viewport.Width / transform.M11, viewport.Height / transform.M22);
        if (_collapsed)
        {
            ResetSyncAnimation();
            ResetCloverPress();
            _shareProgress = 0f;
            _closeAnimation = 0f;
            _closeMesh = null;
            DrawLauncher(spriteBatch, transform, viewportSize);
            return;
        }

        var bounds = new Rectangle(0, 0, viewport.Width, viewport.Height);
        if (_gallery is null || transform != _meshTransform || bounds != _meshBounds)
        {
            _layout = GalleryLayout.Create(viewportSize);
            _gallery = PrepareGallery(_layout, transform, bounds);
            _closeMesh = null;
            PrepareCategoryMeshes(transform, bounds);
            _linearSample = PrepareGradientSample(LinearSample, _layout.LinearSampleOrigin, _layout.Scale, transform, bounds);
            _radialSample = PrepareGradientSample(RadialSample, _layout.RadialSampleOrigin, _layout.Scale, transform, bounds);
            PrepareShareDemo(transform, bounds);
            _iconGrid = null;
            _meshTransform = transform;
            _meshBounds = bounds;
        }

        if (_iconGrid is null)
        {
            _iconGrid = PrepareIconGrid(_layout, transform, bounds);
            _sizeSlider = PrepareSizeSlider(_layout, transform, bounds);
        }

        UpdateDrag(viewportSize);
        var offset = ToPixels(transform);

        var resources = SilkyUISystem.ServiceProvider.GetRequiredService<VectorPathDemoResources>();

        var renderer = resources.GetRenderer(spriteBatch.GraphicsDevice);
        renderer.DrawInUI(spriteBatch, _gallery, Color.White, offset);
        renderer.DrawInUI(spriteBatch, _iconGrid, Color.White, offset);
        renderer.DrawInUI(spriteBatch, _sizeSlider, Color.White, offset);
        DrawCategories(gameTime, spriteBatch, renderer, transform, offset);
        renderer.DrawGradientInUI(spriteBatch, _linearSample, LinearBrush, offset);
        renderer.DrawGradientInUI(spriteBatch, _radialSample, RadialBrush, offset);
        var displayLayout = DisplayLayout();
        DrawCloseButton(gameTime, spriteBatch, renderer, displayLayout, offset);
        DrawSyncIcon(gameTime, spriteBatch, renderer, displayLayout, offset);
        DrawCloverIcon(gameTime, spriteBatch, renderer, offset);
        DrawShareDemo(gameTime, spriteBatch, renderer, displayLayout, offset);
        DrawLabels(spriteBatch, displayLayout);
        if (!_loggedFirstDraw && !_gallery.IsEmpty)
        {
            _loggedFirstDraw = true;
            SilkyUIFramework.Instance.Logger.Debug($"SilkyUI 游戏内图标库已提交首帧，共 {_visibleIcons.Count} 个图标。");
        }
    }

    private void PrepareCategoryMeshes(Matrix transform, Rectangle bounds)
    {
        var firstRow = _layout.Category(0);
        var highlight = new VectorPathBuilder()
            .AddRoundRect(firstRow.Position, firstRow.Size, new Vector2(8f * _layout.Scale)).Build();
        _categoryHighlight = PathRenderer.PrepareFill(highlight, transform, bounds);

        var vertices = new List<VertexPositionColor>();
        var indices = new List<int>();
        for (var i = 0; i < Categories.Length; i++)
        {
            var origin = _layout.Category(i).LeftCenter + new Vector2(10f, -9f) * _layout.Scale;
            var glyphTransform = Matrix.CreateScale(_layout.Scale, _layout.Scale, 1f) *
                Matrix.CreateTranslation(origin.X, origin.Y, 0f) * transform;
            var mesh = PathRenderer.PrepareStroke(Categories[i].Glyph, CategoryGlyphStroke, glyphTransform, bounds);
            var color = i == _category ? AccentColor : TextColor;
            AppendMesh(vertices, indices, mesh, color, color);
        }
        _categoryGlyphs = new PathMesh(vertices.ToArray(), indices.ToArray());
    }

    private void DrawCategories(GameTime gameTime, SpriteBatch spriteBatch, PathRenderer renderer, Matrix transform, Point offset)
    {
        _categorySlideElapsed = Math.Min(_categorySlideElapsed + (float)gameTime.ElapsedGameTime.TotalSeconds, CategorySlideSeconds);
        var remaining = 1f - _categorySlideElapsed / CategorySlideSeconds;
        _categorySlidePosition = MathHelper.Lerp(_categorySlideStart, _category, 1f - remaining * remaining * remaining);

        // 只平移高亮网格，复用它的像素覆盖率。
        var rowStep = _layout.Category(1).Position - _layout.Category(0).Position;
        var slide = Vector2.TransformNormal(rowStep * _categorySlidePosition, transform);
        var highlightOffset = new Point(offset.X + (int)MathF.Round(slide.X), offset.Y + (int)MathF.Round(slide.Y));
        renderer.DrawInUI(spriteBatch, _categoryHighlight, SelectedColor, highlightOffset);
        renderer.DrawInUI(spriteBatch, _categoryGlyphs, Color.White, offset);
    }

    private void DrawCloseButton(GameTime gameTime, SpriteBatch spriteBatch, PathRenderer renderer,
        GalleryLayout layout, Point offset)
    {
        var mouse = new Vector2(Main.mouseX, Main.mouseY);
        var hovered = Main.instance.IsActive && !_dragGrabOffset.HasValue && !_sizeDragging && layout.CloseButton.Contains(mouse);
        var target = hovered ? (_closePressed ? -1f : 1f) : 0f;
        var step = (float)gameTime.ElapsedGameTime.TotalSeconds / 0.12f;
        var previous = _closeAnimation;
        _closeAnimation += Math.Clamp(target - _closeAnimation, -step, step);

        if (_closeMesh is null || _closeAnimation != previous)
        {
            // 只在动画变化时重建这一枚图标的网格。
            var progress = MathF.CopySign(MathHelper.SmoothStep(0f, 1f, MathF.Abs(_closeAnimation)), _closeAnimation);
            var scale = 0.70f * layout.Scale * (1f + 0.14f * progress);
            var center = _layout.CloseButton.Center - new Vector2(0f, 1.5f * progress * layout.Scale);
            var transform = Matrix.CreateTranslation(-24f, -24f, 0f) * Matrix.CreateScale(scale, scale, 1f) *
                Matrix.CreateTranslation(center.X, center.Y, 0f) * _meshTransform;
            _closeMesh = PrepareIcon(VectorPathDemoIcons.Close.Layers, transform, _meshBounds, _closeStrokes);
        }

        renderer.DrawInUI(spriteBatch, _closeMesh, Color.White, offset);
    }

    private void DrawSyncIcon(GameTime gameTime, SpriteBatch spriteBatch, PathRenderer renderer,
        GalleryLayout layout, Point offset)
    {
        if (_syncTile < 0) return;

        var mouse = new Vector2(Main.mouseX, Main.mouseY);

        var hovering = Main.instance.IsActive && !_dragGrabOffset.HasValue && layout.Tile(_syncTile).Contains(mouse);
        if (hovering && !_syncHovered && _syncTurnElapsed >= SyncHalfTurnSeconds)
        {
            _syncTurnStart = _syncAngle;
            _syncTurnElapsed = 0f;
        }
        _syncHovered = hovering;

        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var previousAngle = _syncAngle;
        if (_syncTurnElapsed < SyncHalfTurnSeconds)
        {
            _syncTurnElapsed = Math.Min(_syncTurnElapsed + elapsed, SyncHalfTurnSeconds);
            var remaining = 1f - _syncTurnElapsed / SyncHalfTurnSeconds;
            _syncAngle = (_syncTurnStart + MathHelper.Pi * (1f - remaining * remaining)) % MathHelper.TwoPi;
        }

        var targetScale = _syncPressed ? SyncPressedScale : 1f;

        var scaleStep = elapsed * (1f - SyncPressedScale) / (_syncPressed ? 0.08f : 0.12f);

        var previousScale = _syncScale;
        _syncScale += Math.Clamp(targetScale - _syncScale, -scaleStep, scaleStep);

        if (_syncAngle == 0f && _syncScale == 1f)
        {
            renderer.DrawInUI(spriteBatch, _syncIcon, Color.White, offset);
            return;
        }

        if (_syncAnimatedIcon is null || _syncAngle != previousAngle || _syncScale != previousScale)
        {
            // 先变换矢量路径，再按实际屏幕像素重新计算边缘覆盖率。
            _syncAnimatedIcon = PrepareSyncIcon(_syncTransform, _meshBounds, _syncScale, _syncAngle);
        }
        renderer.DrawInUI(spriteBatch, _syncAnimatedIcon, Color.White, offset);
    }

    private PathMesh PrepareSyncIcon(Matrix iconTransform, Rectangle bounds, float scale, float angle)
    {
        var toCenter = Matrix.CreateTranslation(-24f, -24f, 0f);

        var fromCenter = Matrix.CreateTranslation(24f, 24f, 0f);

        var scaledTransform = Matrix.CreateScale(scale, scale, 1f) * fromCenter * iconTransform;

        var faceTransform = toCenter * scaledTransform;

        var arrowTransform = toCenter * Matrix.CreateRotationZ(angle) * scaledTransform;

        var vertices = new List<VertexPositionColor>();

        var indices = new List<int>();
        foreach (var layer in _visibleIcons[_syncTile].Layers)
        {
            var transform = layer.Animated ? arrowTransform : faceTransform;

            var mesh = layer.Stroke == 0f
                ? PathRenderer.PrepareFill(layer.Path, transform, bounds)
                : PathRenderer.PrepareStroke(layer.Path,
                    new PathStroke(layer.Stroke, PathLineCap.Round, PathLineJoin.Round), transform, bounds);
            AppendMesh(vertices, indices, mesh, layer.Top, layer.Bottom,
                faceTransform.M42 + 4f * faceTransform.M22, 40f * faceTransform.M22);
        }
        return new PathMesh(vertices.ToArray(), indices.ToArray());
    }

    private void ResetSyncAnimation()
    {
        _syncHovered = _syncPressed = false;
        _syncAngle = _syncTurnStart = 0f;
        _syncTurnElapsed = SyncHalfTurnSeconds;
        _syncScale = 1f;
        _syncAnimatedIcon = null;
    }

    private void SetCloverPressed(bool pressed)
    {
        if (_cloverPressed == pressed) return;
        _cloverPressed = pressed;
        _cloverPressStart = _cloverPressProgress;
        _cloverPressElapsed = 0f;
    }

    private void ResetCloverPress()
    {
        _cloverPressed = false;
        _cloverPressStart = _cloverPressProgress = _cloverPressElapsed = 0f;
    }

    private void DrawCloverIcon(GameTime gameTime, SpriteBatch spriteBatch, PathRenderer renderer, Point offset)
    {
        if (_cloverTile < 0) return;

        var duration = _cloverPressed ? 0.16f : 0.26f;
        _cloverPressElapsed = Math.Min(_cloverPressElapsed + (float)gameTime.ElapsedGameTime.TotalSeconds, duration);
        var t = _cloverPressElapsed / duration;
        if (_cloverPressed)
        {
            _cloverPressProgress = MathHelper.SmoothStep(_cloverPressStart, 1f, t);
        }
        else
        {
            // 松开时让绿叶短暂超过原尺寸，再回到原位。
            var remaining = 1f - t;
            _cloverPressProgress = _cloverPressStart * remaining * remaining * (1f - 2.4f * t);
        }

        var pinkOpacity = Math.Clamp(_cloverPressProgress, 0f, 1f);
        if (pinkOpacity < 1f)
        {
            var greenMesh = _cloverGreenMesh;
            if (_cloverPressProgress != 0f)
            {
                var scale = 1f - 0.82f * _cloverPressProgress;
                var transform = Matrix.CreateTranslation(-24f, -24f, 0f) * Matrix.CreateScale(scale, scale, 1f) *
                    Matrix.CreateTranslation(24f, 24f, 0f) * _cloverTransform;
                greenMesh = PrepareIcon(VectorPathDemoIcons.Clover.Layers, transform, _meshBounds, new Dictionary<float, PathStroke>());
            }
            renderer.DrawInUI(spriteBatch, greenMesh, Color.White * (1f - pinkOpacity), offset);
        }
        if (pinkOpacity > 0f)
            renderer.DrawInUI(spriteBatch, _cloverPinkMesh, Color.White * pinkOpacity, offset);
    }

    private static PathMesh PrepareGradientSample(VectorPath path, Vector2 origin, float scale, Matrix transform, Rectangle bounds)
    {
        var sampleTransform = Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(origin.X, origin.Y, 0f) * transform;
        return PathRenderer.PrepareFill(path, sampleTransform, bounds);
    }

    private GalleryBox LauncherBox => new(_launcherPosition.GetValueOrDefault(), LauncherSize);

    private void MoveLauncher(Vector2 mouse)
    {
        if (_launcherPress is not { } press) return;
        var delta = mouse - press.Mouse;
        _launcherDragged |= delta.LengthSquared() >= LauncherDragThreshold * LauncherDragThreshold;
        if (_launcherDragged)
            _launcherPosition = ClampLauncher(press.Position + delta);
    }

    private Vector2 ClampLauncher(Vector2 position)
    {
        var margin = Vector2.Min(new Vector2(6f), Vector2.Max(Vector2.Zero, (_launcherViewport - LauncherSize) / 2f));

        var max = Vector2.Max(margin, _launcherViewport - LauncherSize - margin);
        return Vector2.Clamp(position, margin, max);
    }

    private void DrawLauncher(SpriteBatch spriteBatch, Matrix transform, Vector2 viewportSize)
    {
        _launcherViewport = viewportSize;
        if (SilkyUIInputState.Instance.CurrentMouse[MouseButtonType.Left])
            MoveLauncher(new Vector2(Main.mouseX, Main.mouseY));
        _launcherPosition = ClampLauncher(LauncherBox.Position);
        var scale = new Vector2(transform.M11, transform.M22);
        if (_launcherMesh is null || _launcherScale != scale)
        {
            var localTransform = Matrix.CreateScale(scale.X, scale.Y, 1f);

            var bounds = new Rectangle(0, 0, (int)MathF.Ceiling(LauncherSize.X * scale.X), (int)MathF.Ceiling(LauncherSize.Y * scale.Y));

            var outer = new VectorPathBuilder().AddRoundRect(Vector2.Zero, LauncherSize, new Vector2(13f)).Build();

            var inner = new VectorPathBuilder().AddRoundRect(Vector2.One, LauncherSize - new Vector2(2f), new Vector2(12f)).Build();

            var badge = new VectorPathBuilder().AddRoundRect(new Vector2(6f, 8.5f), new Vector2(25f), new Vector2(8f)).Build();

            var vertices = new List<VertexPositionColor>();

            var indices = new List<int>();
            AppendMesh(vertices, indices, PathRenderer.PrepareFill(outer, localTransform, bounds), OutlineColor, OutlineColor);
            AppendMesh(vertices, indices, PathRenderer.PrepareFill(inner, localTransform, bounds), PanelColor, PanelColor);
            AppendMesh(vertices, indices, PathRenderer.PrepareFill(badge, localTransform, bounds), SelectedColor, SelectedColor);
            AppendMesh(vertices, indices, PathRenderer.PrepareFill(LauncherGlyph, localTransform, bounds), AccentColor, AccentColor);
            AppendMesh(vertices, indices, PathRenderer.PrepareStroke(LauncherChevron, LauncherChevronStroke, localTransform, bounds), HintColor, HintColor);
            _launcherMesh = new PathMesh(vertices.ToArray(), indices.ToArray());
            _launcherScale = scale;
        }

        var pixel = Vector2.Transform(LauncherBox.Position, transform);

        var offset = new Point((int)MathF.Round(pixel.X), (int)MathF.Round(pixel.Y));

        var resources = SilkyUISystem.ServiceProvider.GetRequiredService<VectorPathDemoResources>();
        resources.GetRenderer(spriteBatch.GraphicsDevice).DrawInUI(spriteBatch, _launcherMesh, Color.White, offset);

        var origin = new Vector2((offset.X - transform.M41) / scale.X, (offset.Y - transform.M42) / scale.Y);

        var font = FontAssets.MouseText.Value;

        const float textScale = 0.86f;

        var center = origin + new Vector2(39f, LauncherSize.Y / 2f + TextDrawingHelper.GetFontOffset(font) * textScale);
        spriteBatch.DrawString(font, "图标库", center, TextColor, 0f, new Vector2(0f, font.LineSpacing / 2f), textScale, 0, 0f);
    }

    private Point ToPixels(Matrix transform) =>
        new((int)MathF.Round(_dragOffset.X * transform.M11), (int)MathF.Round(_dragOffset.Y * transform.M22));

    private GalleryLayout DisplayLayout()
    {
        var transform = SilkyUI.TransformMatrix;

        var offset = ToPixels(transform);
        return _layout with { Origin = _layout.Origin + new Vector2(offset.X / transform.M11, offset.Y / transform.M22) };
    }

    private void UpdateDrag(Vector2 viewportSize)
    {
        var origin = _dragGrabOffset is Vector2 grab ? new Vector2(Main.mouseX, Main.mouseY) - grab : _layout.Origin + _dragOffset;

        var margin = Vector2.Min(new Vector2(6f), Vector2.Max(Vector2.Zero, (viewportSize - _layout.Size) / 2f));

        var maxOrigin = Vector2.Max(margin, viewportSize - _layout.Size - margin);
        _dragOffset = Vector2.Clamp(origin, margin, maxOrigin) - _layout.Origin;
    }

    private PathMesh PrepareGallery(GalleryLayout layout, Matrix transform, Rectangle bounds)
    {
        var vertices = new List<VertexPositionColor>();

        var indices = new List<int>();
        FillBox(layout.Panel, 19f, OutlineColor);
        FillBox(layout.Panel.Inset(layout.Scale), 18f, PanelColor);
        FillBox(layout.HeaderDivider, 0f, OutlineColor);
        FillBox(layout.SideDivider, 0f, OutlineColor);
        FillBox(layout.Toolbar, 11f, OutlineColor);
        FillBox(layout.Toolbar.Inset(layout.Scale), 10f, PanelColor);
        FillBox(layout.GradientDivider, 0f, OutlineColor);
        FillBox(layout.LinearSampleCard, 10f, TileColor);
        FillBox(layout.RadialSampleCard, 10f, TileColor);
        FillBox(layout.SizeTrack, 3f, OutlineColor);
        FillBox(layout.SizeTrack.Inset(0.8f * layout.Scale), 2.2f, SelectedColor);
        FillBox(layout.TitleMark, 6f, AccentColor);
        FillBox(layout.TitleMark.Inset(6f * layout.Scale), 2.5f, PanelColor);

        var tiles = new VectorPathBuilder();
        for (var i = 0; i < _visibleIcons.Count; i++)
        {
            var tile = layout.Tile(i);
            tiles.AddRoundRect(tile.Position, tile.Size, new Vector2(14f * layout.Scale));
        }
        Append(PathRenderer.PrepareFill(tiles.Build(), transform, bounds), TileColor, TileColor, 0f, 1f);

        return new PathMesh(vertices.ToArray(), indices.ToArray());

        void FillBox(GalleryBox box, float radius, Color color)
        {
            var path = new VectorPathBuilder().AddRoundRect(box.Position, box.Size, new Vector2(radius * layout.Scale)).Build();
            Append(PathRenderer.PrepareFill(path, transform, bounds), color, color, 0f, 1f);
        }

        void Append(PathMesh mesh, Color top, Color bottom, float startY, float height) =>
            AppendMesh(vertices, indices, mesh, top, bottom, startY, height);
    }

    private PathMesh PrepareIconGrid(GalleryLayout layout, Matrix transform, Rectangle bounds)
    {
        var vertices = new List<VertexPositionColor>();
        var indices = new List<int>();

        // 同步图标单独准备网格，静止时直接复用。
        _syncTile = -1;
        _syncIcon = _syncAnimatedIcon = null;
        _cloverTile = -1;
        _cloverGreenMesh = _cloverPinkMesh = null;
        var styles = new Dictionary<float, PathStroke>();

        var iconSize = _iconSize * layout.Scale;
        for (var i = 0; i < _visibleIcons.Count; i++)
        {
            var tile = layout.Tile(i);

            var origin = tile.Position + new Vector2((tile.Size.X - iconSize) / 2f, (57f * layout.Scale - iconSize) / 2f + 7f * layout.Scale);

            var iconTransform = Matrix.CreateScale(iconSize / 48f, iconSize / 48f, 1f) *
                Matrix.CreateTranslation(origin.X, origin.Y, 0f) * transform;

            var icon = _visibleIcons[i];
            if (ReferenceEquals(icon, VectorPathDemoIcons.Clover))
            {
                _cloverTile = i;
                _cloverTransform = iconTransform;
                _cloverGreenMesh = PrepareIcon(icon.Layers, iconTransform, bounds, styles);
                _cloverPinkMesh = PrepareIcon(VectorPathDemoIcons.PressedCloverLayers, iconTransform, bounds, styles);
                continue;
            }
            if (icon.Layers.Any(layer => layer.Animated))
            {
                _syncTile = i;
                _syncTransform = iconTransform;
                _syncIcon = PrepareSyncIcon(iconTransform, bounds, 1f, 0f);
                continue;
            }
            AppendIconLayers(icon.Layers, iconTransform, bounds, styles, vertices, indices);
        }
        if (_syncTile < 0) ResetSyncAnimation();
        if (_cloverTile < 0) ResetCloverPress();
        return new PathMesh(vertices.ToArray(), indices.ToArray());
    }

    private PathMesh PrepareSizeSlider(GalleryLayout layout, Matrix transform, Rectangle bounds)
    {
        var vertices = new List<VertexPositionColor>();
        var indices = new List<int>();
        var track = layout.SizeTrack;
        var progress = (_iconSize - MinIconSize) / (float)(MaxIconSize - MinIconSize);
        var width = track.Size.X * progress;
        var center = track.LeftCenter + new Vector2(width, 0f);
        var scale = layout.Scale;
        var blue = VectorPathDemoIcons.Blue;

        if (width > 0f)
        {
            var fill = new VectorPathBuilder().AddRoundRect(track.Position,
                new Vector2(width, track.Size.Y), new Vector2(3f * scale)).Build();
            AppendMesh(vertices, indices, PathRenderer.PrepareFill(fill, transform, bounds), blue.Light, blue.Dark,
                Vector2.Transform(track.Position, transform).Y, track.Size.Y * transform.M22);
        }

        // 手柄沿用图标的蓝色渐变、深色外框和浅色内高光。
        FillThumb(center + new Vector2(0f, scale), 8f, blue.Edge * 0.16f, blue.Edge * 0.16f);
        FillThumb(center, 8f, blue.Edge, blue.Edge);
        FillThumb(center, 7f, blue.Light, blue.Dark);
        var highlight = new VectorPathBuilder().AddCircle(center, 6.25f * scale).Build();
        var stroke = new PathStroke(0.75f * scale, PathLineCap.Round, PathLineJoin.Round);
        var highlightColor = Color.White * 0.45f;
        AppendMesh(vertices, indices, PathRenderer.PrepareStroke(highlight, stroke, transform, bounds), highlightColor, highlightColor);
        return new PathMesh(vertices.ToArray(), indices.ToArray());

        void FillThumb(Vector2 position, float radius, Color top, Color bottom)
        {
            var scaledRadius = radius * scale;
            var path = new VectorPathBuilder().AddCircle(position, scaledRadius).Build();
            var topY = Vector2.Transform(position - new Vector2(0f, scaledRadius), transform).Y;
            AppendMesh(vertices, indices, PathRenderer.PrepareFill(path, transform, bounds), top, bottom,
                topY, 2f * scaledRadius * transform.M22);
        }
    }

    private static PathMesh PrepareIcon(VectorPathDemoIcons.Layer[] layers, Matrix iconTransform,
        Rectangle bounds, Dictionary<float, PathStroke> styles)
    {
        var iconVertices = new List<VertexPositionColor>();
        var iconIndices = new List<int>();
        AppendIconLayers(layers, iconTransform, bounds, styles, iconVertices, iconIndices);
        return new PathMesh(iconVertices.ToArray(), iconIndices.ToArray());
    }

    private static void AppendIconLayers(VectorPathDemoIcons.Layer[] layers, Matrix iconTransform,
        Rectangle bounds, Dictionary<float, PathStroke> styles,
        List<VertexPositionColor> targetVertices, List<int> targetIndices)
    {
        foreach (var layer in layers)
        {
            PathMesh mesh;
            if (layer.Stroke == 0f)
                mesh = PathRenderer.PrepareFill(layer.Path, iconTransform, bounds);
            else
            {
                if (!styles.TryGetValue(layer.Stroke, out var stroke))
                {
                    stroke = new PathStroke(layer.Stroke, PathLineCap.Round, PathLineJoin.Round);
                    styles.Add(layer.Stroke, stroke);
                }
                mesh = PathRenderer.PrepareStroke(layer.Path, stroke, iconTransform, bounds);
            }
            AppendMesh(targetVertices, targetIndices, mesh, layer.Top, layer.Bottom,
                iconTransform.M42 + 4f * iconTransform.M22, 40f * iconTransform.M22);
        }
    }

    private static void AppendMesh(List<VertexPositionColor> vertices, List<int> indices, PathMesh mesh,
        Color top, Color bottom, float startY = 0f, float height = 1f)
    {
        var offset = vertices.Count;
        foreach (var vertex in mesh.Vertices)
        {
            var tint = top == bottom ? top : Color.Lerp(top, bottom, Math.Clamp((vertex.Position.Y - startY) / height, 0f, 1f));
            vertices.Add(new VertexPositionColor(vertex.Position, tint * (vertex.Color.A / 255f)));
        }
        foreach (var index in mesh.Indices) indices.Add(offset + index);
    }

    private void DrawLabels(SpriteBatch spriteBatch, GalleryLayout layout)
    {
        var font = FontAssets.MouseText.Value;

        var scale = layout.Scale;

        var header = layout.Header;
        Draw("SilkyUI 图标库", header.LeftCenter + new Vector2(53f * scale, 0f), TextColor, 1.04f);
        Draw("拖动标题栏移动", layout.CloseButton.LeftCenter - new Vector2(12f * scale, 0f), HintColor, 0.76f, 1f);
        Draw("分类", layout.Category(0).LeftCenter + new Vector2(11f, -30.5f) * scale, HintColor, 0.78f);
        for (var i = 0; i < Categories.Length; i++)
        {
            var box = layout.Category(i);
            Draw(Categories[i].Label, box.LeftCenter + new Vector2(CategoryLabelInset * scale, 0f), i == _category ? AccentColor : TextColor, 0.85f);
        }

        Draw("渐变演示", layout.GradientTitle, HintColor, 0.78f);
        Draw("线性渐变", layout.LinearSampleCard.Center + new Vector2(0f, 20f) * scale, TextColor, 0.74f, 0.5f);
        Draw("径向渐变", layout.RadialSampleCard.Center + new Vector2(0f, 26f) * scale, TextColor, 0.74f, 0.5f);

        var toolbar = layout.Toolbar;
        Draw($"{Categories[_category].Label}  ·  {_visibleIcons.Count}", toolbar.LeftCenter + new Vector2(14f * scale, 0f), TextColor, 0.85f);
        Draw("尺寸", layout.SizeSlider.LeftCenter - new Vector2(8f * scale, 0f), HintColor, 0.75f, 1f);
        Draw(_iconSize.ToString(), layout.SizeSlider.RightCenter + new Vector2(20f * scale, 0f), AccentColor, 0.76f, 0.5f);
        for (var i = 0; i < _visibleIcons.Count; i++)
        {
            var tile = layout.Tile(i);
            Draw(_visibleIcons[i].Name, tile.Position + new Vector2(tile.Size.X / 2f, 75f * scale), TextColor, 0.74f, 0.5f);
        }

        void Draw(string text, Vector2 position, Color color, float textScale, float alignment = 0f)
        {
            var measure = font.MeasureString(text);

            var drawScale = textScale * scale;
            position.Y += TextDrawingHelper.GetFontOffset(font) * drawScale;
            spriteBatch.DrawString(font, text, position, color, 0f,
                new Vector2(measure.X * alignment, measure.Y / 2f), drawScale, 0, 0f);
        }
    }

    private sealed record Category(string Label, string GlyphData, params string[] IconNames)
    {
        public VectorPath Glyph { get; } = VectorPath.ParseSvgPathData(GlyphData);
    }

    private readonly record struct GalleryBox(Vector2 Position, Vector2 Size)
    {
        public Vector2 Center => Position + Size / 2f;

        public Vector2 LeftCenter => new(Position.X, Center.Y);

        public Vector2 RightCenter => new(Position.X + Size.X, Center.Y);

        public bool Contains(Vector2 point) => point.X >= Position.X && point.Y >= Position.Y &&
            point.X < Position.X + Size.X && point.Y < Position.Y + Size.Y;

        public GalleryBox Inset(float amount) => new(Position + new Vector2(amount), Size - new Vector2(amount * 2f));
    }

    private readonly record struct GalleryLayout(Vector2 Origin, Vector2 Size, int Columns, float Scale)
    {
        private const float HeaderHeight = 49f;

        private const float SideWidth = 166f;

        private const float ContentX = SideWidth + 17f;

        private const float GridTop = 113f;

        private const float TileWidth = 99f;

        private const float TileHeight = 90f;

        private const float Gap = 9f;

        private const float RadialCardTop = 413f;
        private const float RadialCardHeight = 79f;
        private const float ShareButtonBottomInset = 29f;
        private const float SidebarMinHeight = RadialCardTop + RadialCardHeight + 13f + ShareButtonBottomInset +
            ShareBubbleLift + ShareBubbleExtraLift + ShareBubbleHalfHeight + ShareOuterEdgeWidth + ShareInnerEdgeWidth;

        private float Width => Size.X / Scale;

        private float Height => Size.Y / Scale;

        public static GalleryLayout Create(Vector2 viewport)
        {
            var available = Vector2.Max(Vector2.One, viewport - new Vector2(28f));

            var columns = Math.Clamp((int)((available.X - ContentX - 17f + Gap) / (TileWidth + Gap)), 4, 8);

            var rows = (VectorPathDemoIcons.All.Count + columns - 1) / columns;

            var ideal = new Vector2(ContentX + columns * (TileWidth + Gap) - Gap + 17f,
                Math.Max(GridTop + rows * (TileHeight + Gap) - Gap + 17f, SidebarMinHeight));

            var scale = Math.Min(1.14f, Math.Min(available.X / ideal.X, available.Y / ideal.Y));

            var size = ideal * scale;
            return new GalleryLayout((viewport - size) / 2f, size, columns, scale);
        }

        public GalleryBox Panel => new(Origin, Size);

        public GalleryBox Header => Box(1f, 1f, Width - 2f, HeaderHeight - 1f);

        public GalleryBox CloseButton => new(Header.RightCenter - new Vector2(48f, 17f) * Scale, new Vector2(34f) * Scale);

        public GalleryBox HeaderDivider => Box(1f, HeaderHeight, Width - 2f, 1f);

        public GalleryBox SideDivider => Box(SideWidth, HeaderHeight + 1f, 1f, Height - HeaderHeight - 2f);

        public GalleryBox TitleMark => Box(19f, (HeaderHeight + 1f - 24f) / 2f, 24f, 24f);

        public GalleryBox Toolbar => Box(ContentX, 63f, Width - ContentX - 17f, 39f);

        public GalleryBox Category(int index) => Box(13f, 92f + index * 34f, SideWidth - 26f, 29f);

        public GalleryBox GradientDivider => Box(13f, 305f, SideWidth - 26f, 1f);

        public Vector2 GradientTitle => At(24f, 324f);

        public GalleryBox LinearSampleCard => Box(13f, 341f, SideWidth - 26f, 63f);

        public GalleryBox RadialSampleCard => Box(13f, RadialCardTop, SideWidth - 26f, RadialCardHeight);

        public Vector2 ShareButtonCenter => At(34f, Height - ShareButtonBottomInset);

        public Vector2 LinearSampleOrigin => LinearSampleCard.Position + new Vector2(13f, 8f) * Scale;

        public Vector2 RadialSampleOrigin => RadialSampleCard.Position + new Vector2(48f, 7f) * Scale;

        public GalleryBox SizeSlider => new(
            Toolbar.RightCenter - new Vector2(158f, 13f) * Scale, new Vector2(118f, 26f) * Scale);

        public GalleryBox SizeTrack => new(
            SizeSlider.LeftCenter + new Vector2(8f, -3f) * Scale, new Vector2(102f, 6f) * Scale);

        public GalleryBox Tile(int index) => Box(ContentX + index % Columns * (TileWidth + Gap),
            GridTop + index / Columns * (TileHeight + Gap), TileWidth, TileHeight);

        public Vector2 At(float x, float y) => Origin + new Vector2(x, y) * Scale;

        private GalleryBox Box(float x, float y, float width, float height) => new(At(x, y), new Vector2(width, height) * Scale);
    }
}

[Service]
internal sealed class VectorPathDemoResources : IDisposable
{
    private PathRenderer _renderer;

    public PathRenderer GetRenderer(GraphicsDevice device) => _renderer ??= new PathRenderer(device);

    public void Dispose() => _renderer?.Dispose();
}
