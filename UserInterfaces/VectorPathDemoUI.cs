using SilkyUIFramework.Graphics2D.Paths;
using SilkyUIFramework.Layout;

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

    private static readonly Color ControlBlueLight = new(156, 199, 255);
    private static readonly Color ControlBlueDark = new(89, 152, 248);
    private static readonly Color ControlBlueEdge = new(61, 122, 222);

    private static readonly Color TextColor = new(37, 44, 58);

    private static readonly Color HintColor = new(97, 106, 124);

    private static readonly Vector2 LauncherSize = new(132f, 42f);

    private const float LauncherDragThreshold = 7f;

    private const float CategorySlideSeconds = 0.18f;

    private const float CategoryLabelInset = 34f;

    private static readonly PathStroke CategoryGlyphStroke = new(1.6f, PathLineCap.Round, PathLineJoin.Round);

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
    private PathMesh _sizeSlider;

    private PathMesh _gallery;

    private GalleryIconBatch _animatedIcons;

    private PathMesh _linearSample;

    private PathMesh _radialSample;

    private const float TileWidth = 99f;
    private const float TileHeight = 90f;
    private readonly SUISvgIcon[] _categorySlots = new SUISvgIcon[Categories.Length];
    private readonly SUISvgIcon[] _iconSlots = new SUISvgIcon[VectorPathDemoIcons.All.Count];
    private int _layoutColumns;
    private GalleryLayout _layout;

    private Matrix _meshTransform;

    private Rectangle _meshBounds;

    private Vector2 _dragOffset;

    private Vector2? _dragGrabOffset;

    private bool _loggedFirstDraw;

    private bool _collapsed;

    private bool _closePressed;

    private readonly SUISvgIcon _launcherGlyph = new()
    {
        ViewBox = new Vector4(0f, 0f, 132f, 42f),
        PathData = "M13 15.5H17V19.5H13Z M20 15.5H24V19.5H20Z M13 22.5H17V26.5H13Z M20 22.5H24V26.5H20Z",
        FillColor = AccentColor,
    };

    private readonly SUISvgIcon _launcherChevron = new()
    {
        Icon = new SvgIcon(new Vector4(0f, 0f, 132f, 42f),
            [new SvgIconLayer(VectorPath.ParseSvgPathData("M111 16L116 21L111 26"), HintColor,
                new PathStroke(1.8f, PathLineCap.Round, PathLineJoin.Round))]),
    };

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
        _animatedIcons = null;
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
            _shareProgress = 0f;
            DrawLauncher(spriteBatch, transform, viewportSize);
            return;
        }

        var bounds = new Rectangle(0, 0, viewport.Width, viewport.Height);
        if (_gallery is null || transform != _meshTransform || bounds != _meshBounds)
        {
            _layout = ArrangeGallery(viewportSize);
            _gallery = PrepareGallery(_layout, transform, bounds);
            PrepareCategoryMeshes(transform, bounds);
            _linearSample = PrepareGradientSample(LinearSample, _layout.LinearSampleOrigin, _layout.Scale, transform, bounds);
            _radialSample = PrepareGradientSample(RadialSample, _layout.RadialSampleOrigin, _layout.Scale, transform, bounds);
            PrepareShareDemo(transform, bounds);
            _animatedIcons = null;
            _meshTransform = transform;
            _meshBounds = bounds;
        }

        if (_animatedIcons is null)
        {
            _animatedIcons = PrepareIconGrid(_layout, transform);
            _sizeSlider = PrepareSizeSlider(_layout, transform, bounds);
        }

        UpdateDrag(viewportSize);
        var offset = ToPixels(transform);

        var resources = SilkyUISystem.ServiceProvider.GetRequiredService<PathDrawingResources>();

        var renderer = resources.GetRenderer(spriteBatch.GraphicsDevice);
        renderer.DrawInUI(spriteBatch, _gallery, Color.White, offset);
        var animationTime = gameTime.TotalGameTime.TotalSeconds;
        renderer.DrawInUI(spriteBatch, _animatedIcons.Update(animationTime), Color.White, offset);
        renderer.DrawInUI(spriteBatch, _sizeSlider, Color.White, offset);
        DrawCategories(gameTime, spriteBatch, renderer, transform, offset);
        renderer.DrawGradientInUI(spriteBatch, _linearSample, LinearBrush, offset);
        renderer.DrawGradientInUI(spriteBatch, _radialSample, RadialBrush, offset);
        var displayLayout = DisplayLayout();
        DrawCloseIcon(spriteBatch, renderer, transform, offset);
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
            var color = i == _category ? AccentColor : TextColor;
            var slot = _categorySlots[i];
            slot.Icon = color == AccentColor ? Categories[i].SelectedIcon : Categories[i].NormalIcon;
            AppendIconMesh(vertices, indices, slot.PrepareMesh(new Vector2(18f * _layout.Scale), transform),
                SUISvgIcon.PixelOrigin(origin, transform));
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

    private void DrawCloseIcon(SpriteBatch spriteBatch, PathRenderer renderer, Matrix transform, Point offset)
    {
        var size = new Vector2(33.6f * _layout.Scale);
        var position = _layout.CloseButton.Center - size / 2f;
        var origin = SUISvgIcon.PixelOrigin(position, transform);
        var mesh = CloseLayout.PrepareMesh(size, transform);
        renderer.DrawInUI(spriteBatch, mesh, Color.White, new Point(offset.X + origin.X, offset.Y + origin.Y));
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
            AppendIconMesh(vertices, indices, _launcherGlyph.PrepareMesh(LauncherSize, localTransform), Point.Zero);
            AppendIconMesh(vertices, indices, _launcherChevron.PrepareMesh(LauncherSize, localTransform), Point.Zero);
            _launcherMesh = new PathMesh(vertices.ToArray(), indices.ToArray());
            _launcherScale = scale;
        }

        var pixel = Vector2.Transform(LauncherBox.Position, transform);

        var offset = new Point((int)MathF.Round(pixel.X), (int)MathF.Round(pixel.Y));

        var resources = SilkyUISystem.ServiceProvider.GetRequiredService<PathDrawingResources>();
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

    private GalleryIconBatch PrepareIconGrid(GalleryLayout layout, Matrix transform)
    {
        var size = new Vector2(_iconSize * layout.Scale);
        var parts = new List<IconPart>();
        for (var i = 0; i < _visibleIcons.Count; i++)
        {
            var tile = layout.Tile(i);
            var center = tile.Position + new Vector2(tile.Size.X / 2f, 35.5f * layout.Scale);
            var icon = _visibleIcons[i];
            var slot = _iconSlots[i];
            slot.Icon = icon.Artwork;
            var origin = SUISvgIcon.PixelOrigin(center - size / 2f, transform);
            foreach (var part in icon.Parts)
                parts.Add(new IconPart(slot, part, size, transform, origin, icon.Phase));
        }
        for (var i = _visibleIcons.Count; i < _iconSlots.Length; i++) _iconSlots[i].Icon = null;
        return new GalleryIconBatch(parts);
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

        if (width > 0f)
        {
            var fill = new VectorPathBuilder().AddRoundRect(track.Position,
                new Vector2(width, track.Size.Y), new Vector2(3f * scale)).Build();
            AppendMesh(vertices, indices, PathRenderer.PrepareFill(fill, transform, bounds), ControlBlueLight, ControlBlueDark,
                Vector2.Transform(track.Position, transform).Y, track.Size.Y * transform.M22);
        }

        // 手柄沿用图标的蓝色渐变、深色外框和浅色内高光。
        FillThumb(center + new Vector2(0f, scale), 8f, ControlBlueEdge * 0.16f, ControlBlueEdge * 0.16f);
        FillThumb(center, 8f, ControlBlueEdge, ControlBlueEdge);
        FillThumb(center, 7f, ControlBlueLight, ControlBlueDark);
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

    private static void AppendIconMesh(List<VertexPositionColor> vertices, List<int> indices, PathMesh mesh, Point position)
    {
        var start = vertices.Count;
        var offset = new Vector3(position.X, position.Y, 0f);
        foreach (var vertex in mesh.Vertices) vertices.Add(new VertexPositionColor(vertex.Position + offset, vertex.Color));
        foreach (var index in mesh.Indices) indices.Add(start + index);
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
        private readonly VectorPath _glyph = VectorPath.ParseSvgPathData(GlyphData);

        public SvgIcon NormalIcon => field ??= new SvgIcon(new Vector4(0f, 0f, 18f, 18f),
            [new SvgIconLayer(_glyph, TextColor, CategoryGlyphStroke)]);

        public SvgIcon SelectedIcon => field ??= new SvgIcon(new Vector4(0f, 0f, 18f, 18f),
            [new SvgIconLayer(_glyph, AccentColor, CategoryGlyphStroke)]);
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

    private void InitializeGallerySlots()
    {
        CloseLayout.Icon = VectorPathDemoIcons.Close.Artwork;
        CloseLayout.DrawFromParent = true;
        for (var i = 0; i < _categorySlots.Length; i++)
        {
            var slot = new SUISvgIcon { Width = new Dimension(0f, 1f), Height = new Dimension(29f), FlexShrink = 0f, DrawFromParent = true };
            CategoryLayout.AddChild(slot);
            _categorySlots[i] = slot;
        }

        for (var i = 0; i < _iconSlots.Length; i++)
        {
            var slot = new SUISvgIcon { Width = new Dimension(0f, 1f), Height = new Dimension(0f, 1f), DrawFromParent = true };
            IconGridLayout.AddChild(slot);
            _iconSlots[i] = slot;
        }
    }

    private GalleryLayout ArrangeGallery(Vector2 viewport)
    {
        var available = Vector2.Max(Vector2.One, viewport - new Vector2(28f));
        var gap = IconGridLayout.Gap;
        var horizontalInset = -ContentLayout.Width.Pixels;
        var columns = Math.Clamp((int)((available.X - horizontalInset + gap.Width) / (TileWidth + gap.Width)), 4, 8);
        var rows = (_iconSlots.Length + columns - 1) / columns;
        var radialBottom = SidebarLayout.Top.Pixels + RadialLayout.Top.Pixels + RadialLayout.Height.Pixels;
        var shareBottomInset = -SidebarLayout.Top.Pixels - SidebarLayout.Height.Pixels - ShareLayout.Top.Pixels +
            ShareLayout.Height.Pixels / 2f;
        var tooltipRise = ShareBubbleLift + ShareBubbleExtraLift + ShareBubbleHalfHeight + ShareOuterEdgeWidth + ShareInnerEdgeWidth;
        var sidebarHeight = radialBottom + 13f + shareBottomInset + tooltipRise;
        var ideal = new Vector2(horizontalInset + columns * TileWidth + (columns - 1) * gap.Width,
            Math.Max(IconGridLayout.Top.Pixels - ContentLayout.Height.Pixels + rows * TileHeight + (rows - 1) * gap.Height, sidebarHeight));

        if (_layoutColumns != columns)
        {
            IconGridLayout.SetTemplateColumns(Enumerable.Repeat(GridTrack.Pixels(TileWidth), columns).ToArray());
            IconGridLayout.SetTemplateRows(Enumerable.Repeat(GridTrack.Pixels(TileHeight), rows).ToArray());
            _layoutColumns = columns;
        }

        SetSize(ideal.X, ideal.Y);
        UpdateLayout();
        RecalculatePosition();

        var scale = Math.Min(1.14f, Math.Min(available.X / ideal.X, available.Y / ideal.Y));
        var size = ideal * scale;
        return new GalleryLayout(this, (viewport - size) / 2f, size, scale);
    }

    private readonly record struct GalleryLayout(VectorPathDemoUI View, Vector2 Origin, Vector2 Size, float Scale)
    {
        public GalleryBox Panel => new(Origin, Size);
        public GalleryBox Header => Box(View.HeaderLayout);
        public GalleryBox CloseButton => Box(View.CloseLayout);
        public GalleryBox HeaderDivider => Box(View.HeaderDividerLayout);
        public GalleryBox SideDivider => Box(View.SideDividerLayout);
        public GalleryBox TitleMark => Box(View.TitleMarkLayout);
        public GalleryBox Toolbar => Box(View.ToolbarLayout);
        public GalleryBox Category(int index) => Box(View._categorySlots[index]);
        public GalleryBox GradientDivider => Box(View.GradientDividerLayout);
        public Vector2 GradientTitle => Box(View.GradientTitleLayout).Position;
        public GalleryBox LinearSampleCard => Box(View.LinearLayout);
        public GalleryBox RadialSampleCard => Box(View.RadialLayout);
        public Vector2 ShareButtonCenter => Box(View.ShareLayout).Center;
        public Vector2 LinearSampleOrigin => LinearSampleCard.Position + new Vector2(13f, 8f) * Scale;
        public Vector2 RadialSampleOrigin => RadialSampleCard.Position + new Vector2(48f, 7f) * Scale;
        public GalleryBox SizeSlider => Box(View.SizeSliderLayout);
        public GalleryBox SizeTrack => Box(View.SizeTrackLayout);
        public GalleryBox Tile(int index) => Box(View._iconSlots[index]);

        private GalleryBox Box(UIView view)
        {
            var bounds = view.Bounds;
            return new GalleryBox(Origin + (bounds.Position - View.InnerBounds.Position) * Scale,
                new Vector2(bounds.Width, bounds.Height) * Scale);
        }
    }
}
