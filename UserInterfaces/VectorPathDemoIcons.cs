using SilkyUIFramework.Graphics2D.Paths;
using Layer = SilkyUIFramework.Graphics2D.Paths.SvgIconLayer;

namespace SilkyUIFramework.UserInterfaces;

internal static class VectorPathDemoIcons
{
    private static readonly Vector4 ViewBox = new(0f, -2f, 31f, 29f);
    private static readonly Vector2 Center = new(15.5f, 12.5f);
    private static readonly Color White = Color.White;
    private static readonly Palette Orange = new(new Color(230, 116, 49));
    private static readonly Palette Purple = new(new Color(136, 96, 222));
    private static readonly Palette Pink = new(new Color(217, 68, 139));
    private static readonly Palette Green = new(new Color(39, 153, 88));
    private static readonly Palette Blue = new(new Color(83, 147, 255));

    private static readonly VectorPath Square = Box(5.2f, 2.2f, 20.6f, 20.6f);
    private static readonly VectorPath Wide = Box(4f, 3.3f, 23f, 18.4f);
    private static readonly VectorPath Disc = Circle(15.5f, 12.5f, 11.5f);
    private static readonly VectorPath Screen = Box(7f, 6.3f, 17f, 12.4f, 1.2f);

    internal static Icon Close { get; } = new("关闭", Still(SquareWithBottomBand(Pink)),
        Still(Line(White, 2.2f, "M12 9L19 16M19 9L12 16")));

    internal static IReadOnlyList<Icon> All { get; } =
    [
        new("数据看板", Still(Body(Wide, Orange)), Still(Fill(White, Screen)),
            Move(new(0, -2.5f), Fill(Orange.Mark, Box(9, 12.2f, 4.1f, 4.5f, 1.2f))),
            Still(Fill(Orange.Soft, Box(15.5f, 8.3f, 6.5f, 2.5f, 1.2f)),
                Fill(Orange.Mark, Box(15.5f, 13.3f, 6.5f, 3.4f, 1.2f)))),
        Plot("折线图", Purple, "M9 16L13 12L16.8 13.8L21.7 9", 2f),
        new("图表", Still(SquareWithBottomBand(Pink)),
            Stretch(new(10.5f, 18.8f), new(0, 0.6f), Fill(White, Box(8.8f, 13.6f, 3.4f, 5.2f, 1.2f))),
            Stretch(new(15.5f, 18.8f), new(0, -0.35f), Fill(White, Box(13.8f, 8.4f, 3.4f, 10.4f, 1.2f))),
            Stretch(new(20.5f, 18.8f), new(0, 0.48f), Fill(White, Box(18.8f, 11.4f, 3.4f, 7.4f, 1.2f)))),
        new("饼图", Still(Body(D("M15.5 12.5L15.5 2A10.5 10.5 0 1 0 24.59 17.75Z"), Green)),
            Move(new(2.5f, -1.4f), Body(D("M15.5 12.5V2A10.5 10.5 0 0 1 24.59 17.75Z"), Green, front: true, shadow: false))),
        new("环形图", Still(Body(Ring(11.5f, 5.8f), Blue)),
            Turn(Center, 36f, Fill(Blue.Mark, "M15.5 1A11.5 11.5 0 0 1 25.46 18.25L20.52 15.4A5.8 5.8 0 0 0 15.5 6.7Z"))),
        new("面积图", Still(Body(Wide, Blue)), Still(Fill(White, Screen)),
            Stretch(new(15.5f, 16.7f), new(0, 0.27f), Fill(Blue.Mark, "M9 16.7V13.3L12.1 11.2L16 13.2L22 10V16.7Z"))),
        new("漏斗图", Still(Body(Square, Orange)),
            Still(Fill(White, "M9 7H22L20.3 10.6H10.7Z M12 13H19L17.8 15.8H13.2Z")),
            Move(new(0, 1.7f), Fill(Orange.Mark, Box(14f, 17f, 3f, 2.5f, 1.2f)))),
        new("动态", Still(Body(Wide, Green)), Still(Fill(White, Screen)),
            Move(new(2.6f, 0), Line(Green.Mark, 1.6f, "M9 13H11L12.8 9.5L15 15.7L16.8 12H19.2"))),
        Trend("上涨", Orange, true),
        Trend("下跌", Orange, false),
        new("仪表", Still(Body(D("M4 17V15.7A11.5 11.5 0 0 1 27 15.7V17Q27 19.7 24.3 19.7H6.7Q4 19.7 4 17Z"), Blue)),
            Still(Fill(White, "M7.4 16A8.1 8.1 0 0 1 23.6 16Z")),
            Turn(new(15.5f, 16f), 57f, Line(Blue.Mark, 2f, "M15.5 16L11 12.6")),
            Still(Fill(Blue.Soft, Circle(15.5f, 16f, 1.45f)))),
        GridIcon("热力图", Purple, true),
        new("多人", Still(Body(D("M7 20Q7 15 11.6 15H18.6Q23 15 23 20V22H7Z"), Green, front: true)),
            Move(new(-2.2f, 0), Body(Circle(11, 9, 4), Green, shadow: false)),
            Move(new(2.2f, 0), Body(Circle(20, 8, 4), Green, front: true, shadow: false))),
        new("用户", Still(Body(Disc, Pink)),
            Still(Fill(White, "M8.9 18.2Q9.5 14.9 13.5 14.9H17.5Q21.5 14.9 22.1 18.2Q19.5 21.2 15.5 21.2Q11.5 21.2 8.9 18.2Z")),
            Move(new(2.4f, 0), Fill(White, Circle(14.3f, 9f, 3.4f)))),
        new("设置", Still(Body(Disc, Purple)), Still(Fill(White, Circle(15.5f, 12.5f, 8.3f))),
            Turn(Center, 60f, Fill(Purple.Mark, Gear()), Fill(White, Circle(15.5f, 12.5f, 2.1f)))),
        new("调节", Still(Body(Wide, Orange)),
            Still(Line(White, 1.6f, "M8.5 9H22.5M8.5 16H22.5")),
            Move(new(6, 0), Body(Circle(12, 9, 2.65f), Orange, front: true, shadow: false)),
            Move(new(-5, 0), Body(Circle(19, 16, 2.65f), Orange, front: true, shadow: false))),
        Funnel("筛选", Pink),
        new("搜索", Still(Body(Box(5.5f, 1, 17, 23), Orange)),
            Still(Line(White, 1.6f, "M8.7 6.5H19M8.7 10.5H15.3")),
            Move(new(-2.5f, -1.7f), Fill(White, Circle(20.3f, 16.5f, 3.9f)),
                Line(Orange.Mark, 1.6f, Circle(20.3f, 16.5f, 4.3f)),
                Line(Orange.Mark, 2.2f, "M23.4 19.6L27 23.2"))),
        new("通知", Still(Body(Disc, Orange)),
            Turn(new(15.5f, 7.5f), 17f, Fill(White, "M9.2 16.7Q11 15.5 11 12.8V11.3A4.5 4.5 0 0 1 20 11.3V12.8Q20 15.5 21.8 16.7V18.3H9.2Z"),
                Fill(Orange.Mark, Circle(15.5f, 19.2f, 1.65f)))),
        new("日历", Still(Body(Square, Pink)),
            Still(Fill(White, Box(8.1f, 9.1f, 14.8f, 10.8f, 1.2f)), Line(Pink.Soft, 1.6f, "M11 13H20M11 16.4H17")),
            Stretch(new(15.5f, 9.1f), new(0, -0.92f), Fill(White, Box(8.1f, 9.1f, 14.8f, 10.8f, 1.2f)),
                Line(Pink.Mark, 1.6f, "M11.4 13H15.2M11.4 16.4H19.6")),
            Still(Fill(Pink.Light, Box(5.2f, 2.2f, 20.6f, 6.4f)),
                Line(Pink.Mark, 1.6f, "M10.5 2.2V5.6M20.5 2.2V5.6"))),
        new("收件箱", Still(Body(Wide, Blue)),
            Move(new(0, -2.8f), [.. Body(Box(8.5f, 6.6f, 14, 11, 1.2f), Blue, front: true, shadow: false),
                Line(Blue.Mark, 1.6f, "M12 10H19")]),
            Still(Body(D("M4 13H10.6L12 15.5H19L20.4 13H27V19Q27 21.7 24.3 21.7H6.7Q4 21.7 4 19Z"), Blue, front: true, shadow: false))),
        new("数据库", Still(Body(D("M6.3 5.2H24.7V19.7C24.7 25 6.3 25 6.3 19.7Z"), Pink)),
            Still(Line(Pink.Mark, 1.6f, "M8.7 11.8C11.3 14.9 19.7 14.9 22.3 11.8M8.7 16.8C11.3 19.9 19.7 19.9 22.3 16.8")),
            Move(new(0, 1.8f), Body(Oval(15.5f, 5.4f, 9.2f, 3.5f), Pink, front: true, shadow: false))),
        new("服务器", Still(Body(Box(4, 4.3f, 23, 16.4f), Pink)),
            Still([.. Body(Box(5.5f, 5.8f, 20, 6.2f, 1.7f), Pink, front: true, shadow: false),
                .. Body(Box(5.5f, 13, 20, 6.2f, 1.7f), Pink, front: true, shadow: false)]),
            Move(new(3, 0), Fill(Pink.Mark, Circle(11, 8.9f, 1.3f))),
            Move(new(-3, 0), Fill(Pink.Mark, Circle(19.5f, 16.1f, 1.3f)))),
        new("报表文件", Still(Body(D("M8.5 1H18.5L24.7 7.2V21.8Q24.7 24 22.5 24H8.5Q6.3 24 6.3 21.8V3.2Q6.3 1 8.5 1Z"), Green)),
            Still(Fill(Green.Light, "M18.5 1L24.7 7.2H19.7Q18.5 7.2 18.5 6Z")),
            Stretch(new(11.2f, 19.6f), new(0, 0.45f), Fill(White, Box(9.8f, 14.6f, 2.8f, 5f, 1.2f))),
            Still(Fill(White, Box(14.9f, 11.6f, 2.8f, 8f, 1.2f)), Fill(White, Box(20, 13.7f, 2.8f, 5.9f, 1.2f)))),
        new("演示", Still(Body(Wide, Purple)), Still(Fill(White, Screen)),
            Move(new(3, 0), Fill(Purple.Mark, "M13 9.1L18.8 12.5L13 15.9Z")),
            Still(Fill(Purple.Light, Box(4.2f, 3.3f, 22.6f, 2.6f, 1.2f)))),
        new("看板", Still(Body(Wide, Purple)), Still(Fill(White, Screen)),
            Still(Fill(Purple.Soft, Box(9, 8.7f, 3.4f, 7.6f, 1.2f)), Fill(Purple.Soft, Box(18.6f, 8.7f, 3.4f, 7.6f, 1.2f))),
            Move(new(4.8f, 0), Fill(Purple.Mark, Box(9, 10.5f, 3.4f, 4.2f, 1.2f)))),
        new("侧边栏", Still(Body(Wide, Orange)), Still(Fill(White, Screen)),
            Stretch(new(7, 12.5f), new(0.67f, 0), Fill(Orange.Light, Box(7, 6.3f, 5.4f, 12.4f, 1.2f))),
            Still(Line(Orange.Mark, 1.6f, "M16 10.2H21.4M16 14.7H19.6"))),
        new("列表视图", Still(Body(Wide, Pink)), Still(Fill(White, Screen)),
            Move(new(2.4f, 0), Line(Pink.Mark, 1.6f, "M9.8 9.7H18")),
            Still(Line(Pink.Soft, 1.6f, "M9.8 15.3H20.8"))),
        GridIcon("网格", Purple, false),
        new("下载", Still(Body(Disc, Blue)), Still(Fill(Blue.Light, "M7.6 16.3H23.4Q21 21 15.5 21Q10 21 7.6 16.3Z")),
            Move(new(0, 2.4f), Arrow(White, 15.5f, 5.4f, 6.2f, 1))),
        Transfer("导出", Blue, -1),
        new("同步", Still(Body(Disc, Pink)), Still(Fill(White, Circle(15.5f, 12.5f, 8.3f))),
            Turn(Center, 360f, Line(Pink.Mark, 1.6f, "M10.7 11.4A5 5 0 0 1 18.5 8.7M20.3 13.6A5 5 0 0 1 12.5 16.3"),
                Fill(Pink.Mark, "M19.6 6.6L20.1 11L15.8 9.8Z M11.4 18.4L10.9 14L15.2 15.2Z"))),
        new("完成", Still(Body(Disc, Orange)),
            Stretch(new(15.5f, 12.5f), new(0.18f, 0.18f), Line(White, 2.2f, "M10.3 12.4L13.9 16L20.7 9.2"))),
        new("警告", Still(Body(D("M13.6 3.2Q15.5 0.1 17.4 3.2L26.5 19.6Q27.9 22.4 24.6 22.4H6.4Q3.1 22.4 4.5 19.6Z"), Purple)),
            Move(new(0, -2.4f), Line(White, 2.2f, "M15.5 9.4V13.2")), Still(Fill(White, Circle(15.5f, 18.2f, 1.3f)))),
        new("更多", Still(Body(Box(4, 5.4f, 23, 14.2f, 7.1f), Green)),
            Move(new(0, -2.2f), Fill(White, Circle(9.9f, 12.5f, 1.35f))),
            Move(new(0, -2.8f), 0.07f, Fill(White, Circle(15.5f, 12.5f, 1.35f))),
            Move(new(0, -2.2f), 0.14f, Fill(White, Circle(21.1f, 12.5f, 1.35f)))),
        Transfer("全部拿走", Orange, -1),
        Transfer("全部放入", Blue, 1),
        new("快速堆叠", Still(Body(Square, Green)),
            Still(Fill(Green.Light, Box(8.4f, 15.5f, 8.6f, 3.4f, 1.2f))),
            Move(new(0, 2.7f), Fill(White, Box(8.4f, 8, 8.6f, 3.4f, 1.2f))),
            Move(new(0, 2.4f), Arrow(White, 21, 7.4f, 8.1f, 1))),
        new("排序", Still(Body(Square, Purple)),
            Still(Line(White, 1.6f, "M9 8H15M9 12.5H13.8M9 17H12.5")),
            Move(new(0, 2.5f), Arrow(White, 20, 7, 9.2f, 1))),
        new("四叶草", Still(Body(Disc, Green)),
            Stretch(Center, new(0.28f, 0.28f), Fill(White, CloverLeaves())),
            Still(Fill(Green.Mark, Circle(15.5f, 12.5f, 1.3f)))),
    ];

    private static Icon Plot(string name, Palette palette, string curve, float lift) =>
        new(name, Still(Body(Wide, palette)), Still(Fill(White, Screen)), Move(new(0, -lift), Line(palette.Mark, 1.6f, curve)));

    private static Icon Trend(string name, Palette palette, bool rises)
    {
        var path = rises ? "M9 16.1L13.7 12.2L16.9 13.5L19.6 10.7" : "M9 8.9L13.7 12.8L16.9 11.5L19.6 14.3";
        var head = rises ? "M22 8.3L21.3 13L17.3 9Z" : "M22 16.7L17.3 16L21.3 12Z";
        return new Icon(name, Still(Body(Wide, palette)), Still(Fill(White, Screen)),
            Move(new(1f, rises ? -1.4f : 1.4f), Line(palette.Mark, 1.6f, path), Fill(palette.Mark, head)));
    }

    private static Icon Funnel(string name, Palette palette) => new(name,
        Still(Body(D("M5.5 2Q4 2 4.9 3.5L11.6 12H19.4L26.1 3.5Q27 2 25.5 2Z"), palette)),
        Still(Body(D("M11.9 11H19.1V19.9Q19.1 21 18.1 21.5L13.5 23.6Q11.9 24.2 11.9 22.5Z"), palette, front: true, shadow: false)),
        Stretch(new(15.5f, 6.5f), new(-0.35f, 0), Line(White, 1.6f, "M10.4 6.5H20.6")));

    private static Icon GridIcon(string name, Palette palette, bool heat)
    {
        var groups = new List<Group> { Still(Body(Square, palette)) };
        for (var y = 0; y < 2; y++)
        for (var x = 0; x < 2; x++)
        {
            var position = new Vector2(9f + x * 7.6f, 6f + y * 7.6f);
            var color = heat && x == y ? palette.Light : White;
            groups.Add(Stretch(position + new Vector2(2.7f), new Vector2(heat ? -0.35f : -0.28f),
                (x + y * 2) * 0.065f, Fill(color, Box(position.X, position.Y, 5.4f, 5.4f, 1.2f))));
        }
        return new Icon(name, groups.ToArray());
    }

    private static Icon Transfer(string name, Palette palette, int direction) => new(name,
        Still(Body(Wide, palette)),
        Still(Fill(palette.Light, "M7.8 14.4H11.3L13 16.8H18L19.7 14.4H23.2V18.6Q23.2 19.8 22 19.8H9Q7.8 19.8 7.8 18.6Z")),
        Move(new(0, direction * 2.4f), Arrow(White, 15.5f, direction > 0 ? 5.9f : 13.9f, 7.1f, direction)));

    private static Layer[] Arrow(Color color, float x, float y, float length, int direction)
    {
        var tip = y + length * direction;
        var shoulder = tip - 3.7f * direction;
        var shaft = new VectorPathBuilder().MoveTo(new Vector2(x, y)).LineTo(new Vector2(x, shoulder)).Build();
        return [new Layer(shaft, color, new PathStroke(2.2f, PathLineCap.Butt)),
            Fill(color, Polygon(new(x - 3.2f, shoulder), new(x + 3.2f, shoulder), new(x, tip)))];
    }

    private static Layer[] SquareWithBottomBand(Palette palette)
    {
        const float radius = 2.2f;
        const float bottom = 22.8f;
        const float bandHeight = 1.2f;
        var angle = MathF.Asin((radius - bandHeight) / radius);
        var left = new Vector2(5.2f + radius, bottom - radius);
        var right = new Vector2(25.8f - radius, bottom - radius);
        var radii = new Vector2(radius);
        var band = new VectorPathBuilder()
            .MoveTo(new Vector2(left.X - radius * MathF.Cos(angle), bottom - bandHeight))
            .ArcTo(right, radii, angle, MathHelper.PiOver2 - angle)
            .ArcTo(left, radii, MathHelper.PiOver2, MathHelper.PiOver2 - angle)
            .Close().Build();
        return [.. Body(Square, palette), Fill(palette.Band, band)];
    }

    private static Layer[] Body(VectorPath path, Palette palette, bool front = false, bool shadow = true)
    {
        var outline = new Layer(path, palette.Edge, StrokeStyles.Outside);
        var top = front ? palette.Light : palette.Back;
        var bottom = front ? palette.FrontBottom : palette.Dark;
        var face = new Layer(path, Color.Lerp(top, White, 0.10f), BottomColor: bottom);
        if (!shadow) return [outline, face];
        var displaced = path.Transform(Matrix.CreateTranslation(0f, 2f, 0f));
        return [new Layer(displaced, palette.Shadow * 0.045f, StrokeStyles.ShadowSoft),
            new Layer(displaced, palette.Shadow * 0.075f, StrokeStyles.ShadowNear),
            Fill(palette.Shadow * 0.10f, displaced), outline, face];
    }

    private static class StrokeStyles
    {
        public static readonly PathStroke Outside = new(2f, PathLineCap.Round, PathLineJoin.Round);
        public static readonly PathStroke ShadowSoft = new(4f, PathLineCap.Round, PathLineJoin.Round);
        public static readonly PathStroke ShadowNear = new(2.4f, PathLineCap.Round, PathLineJoin.Round);
    }

    private static Group Still(params Layer[] layers) => new(layers, default);
    private static Group Move(Vector2 shift, params Layer[] layers) => Move(shift, 0f, layers);
    private static Group Move(Vector2 shift, float delay, params Layer[] layers) => new(layers, new Motion(default, shift, default, 0f, delay));
    private static Group Turn(Vector2 pivot, float degrees, params Layer[] layers) =>
        new(layers, new Motion(pivot, default, default, MathHelper.ToRadians(degrees), 0f, degrees == 360f));
    private static Group Stretch(Vector2 pivot, Vector2 scale, params Layer[] layers) => Stretch(pivot, scale, 0f, layers);
    private static Group Stretch(Vector2 pivot, Vector2 scale, float delay, params Layer[] layers) =>
        new(layers, new Motion(pivot, default, scale, 0f, delay));

    private static Layer Fill(Color color, string data) => Fill(color, D(data));
    private static Layer Fill(Color color, VectorPath path) => new(path, color);
    private static Layer Line(Color color, float width, string data) => Line(color, width, D(data));
    private static Layer Line(Color color, float width, VectorPath path) => new(path, color, new PathStroke(width, PathLineCap.Round, PathLineJoin.Round));
    private static VectorPath D(string data) => VectorPath.ParseSvgPathData(data);
    private static VectorPath Box(float x, float y, float width, float height, float radius = 2.2f) =>
        new VectorPathBuilder().AddRoundRect(new(x, y), new(width, height), new Vector2(radius)).Build();
    private static VectorPath Circle(float x, float y, float radius) => Oval(x, y, radius, radius);
    private static VectorPath Oval(float x, float y, float rx, float ry) =>
        new VectorPathBuilder().AddEllipse(new(x, y), new(rx, ry)).Build();
    private static VectorPath Ring(float outer, float inner) => new VectorPathBuilder { FillRule = PathFillRule.EvenOdd }
        .AddCircle(Center, outer).AddCircle(Center, inner).Build();
    private static VectorPath Polygon(params Vector2[] points)
    {
        var path = new VectorPathBuilder().MoveTo(points[0]);
        foreach (var point in points.AsSpan(1)) path.LineTo(point);
        return path.Close().Build();
    }

    private static VectorPath Gear()
    {
        var points = new Vector2[24];
        for (var i = 0; i < points.Length; i++)
        {
            var angle = MathHelper.TwoPi * i / points.Length;
            var radius = i % 4 is 1 or 2 ? 6.6f : 4.9f;
            points[i] = Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }
        return Polygon(points);
    }

    private static VectorPath CloverLeaves()
    {
        var path = new VectorPathBuilder();
        var leaf = D("M15.5 11.1C10.4 7.5 11.9 4.7 14 5.7L15.5 6.6L17 5.7C19.1 4.7 20.6 7.5 15.5 11.1Z");
        for (var i = 0; i < 4; i++)
            path.AddPath(leaf, Matrix.CreateTranslation(-Center.X, -Center.Y, 0f) *
                Matrix.CreateRotationZ(i * MathHelper.PiOver2) * Matrix.CreateTranslation(Center.X, Center.Y, 0f));
        return path.Build();
    }

    internal sealed class Icon
    {
        public string Name { get; }
        public SvgIcon Artwork { get; }
        public Part[] Parts { get; }
        public double Phase { get; }

        public Icon(string name, params Group[] groups)
        {
            Name = name;
            var layers = new List<Layer>();
            var parts = new List<Part>();
            foreach (var group in groups)
            {
                parts.Add(new Part(layers.Count, group.Layers.Length, group.Motion));
                layers.AddRange(group.Layers);
            }
            Artwork = new SvgIcon(ViewBox, layers);
            Parts = parts.ToArray();
            var phase = 0;
            foreach (var character in name) phase = (phase * 31 + character) % 137;
            Phase = phase * 0.013;
        }
    }

    internal readonly record struct Group(Layer[] Layers, Motion Motion);
    internal readonly record struct Part(int FirstLayer, int LayerCount, Motion Motion);
    internal readonly record struct Motion(Vector2 Pivot, Vector2 Shift, Vector2 ScaleDelta, float Angle, float Delay, bool Repeat = false)
    {
        public bool IsMoving => Shift != Vector2.Zero || ScaleDelta != Vector2.Zero || Angle != 0f;
    }

    private readonly record struct Palette(Color Edge)
    {
        public Color Light => Shade(new Color(204, 223, 255));
        public Color FrontBottom => Shade(new Color(178, 206, 255));
        public Color Back => Shade(new Color(147, 187, 255));
        public Color Dark => Shade(new Color(129, 176, 255));
        public Color Shadow => Shade(new Color(138, 182, 255));
        public Color Band => Shade(new Color(67, 137, 255));
        public Color Mark => Shade(new Color(47, 111, 224));
        public Color Soft => Shade(new Color(137, 181, 255));

        private Color Shade(Color source)
        {
            var red = (source.R - 83f) / 172f;
            var green = (source.G - 147f) / 108f;
            return new Color(Channel(Edge.R, red), Channel(Edge.G, green), Channel(Edge.B, red));
        }

        private static int Channel(byte value, float amount) =>
            (int)MathF.Round(amount >= 0f ? value + (255 - value) * amount : value * (1f + amount));
    }
}
