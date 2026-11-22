using SilkyUIFramework.Graphics2D.Paths;

namespace SilkyUIFramework.UserInterfaces;

// 每个图标使用 48 × 48 的局部坐标。图层按从后向前的顺序提交。
internal static class VectorPathDemoIcons
{
    private static readonly Palette Orange = new(new Color(255, 180, 120), new Color(255, 127, 43), new Color(218, 94, 20));

    private static readonly Palette Purple = new(new Color(198, 166, 255), new Color(150, 104, 244), new Color(117, 77, 220));

    private static readonly Palette Pink = new(new Color(255, 164, 205), new Color(237, 88, 157), new Color(218, 59, 132));

    private static readonly Palette Green = new(new Color(146, 230, 174), new Color(63, 198, 111), new Color(29, 159, 77));

    internal static readonly Palette Blue = new(new Color(156, 199, 255), new Color(89, 152, 248), new Color(61, 122, 222));

    private static readonly Color White = Color.White;

    private static readonly Color Highlight = Color.White * 0.45f;

    private static readonly Color PaleBlue = new(193, 220, 255);

    private static readonly Color PaleGreen = new(172, 235, 187);

    private static readonly Color PalePink = new(255, 190, 222);

    private static readonly Color PaleOrange = new(255, 208, 162);

    private static readonly VectorPath CloverLeaf = Svg("M24 22C21 18 15 14 16 9C16.5 4 22 3 24 7C26 3 31.5 4 32 9C33 14 27 18 24 22Z");

    private static readonly VectorPath CloverOutline = RepeatCloverLeaf(CloverLeaf);

    private static readonly VectorPath CloverInset = RepeatCloverLeaf(CloverLeaf.Transform(
        Matrix.CreateTranslation(-24f, -12f, 0f) * Matrix.CreateScale(0.77f, 0.77f, 1f) *
        Matrix.CreateTranslation(24f, 12f, 0f)));

    internal static Icon Clover { get; } = new("四叶草", Face(CloverOutline, Green, Line(Highlight, 1f, CloverInset)));

    internal static Layer[] PressedCloverLayers { get; } = Face(CloverOutline, Pink, Line(Highlight, 1f, CloverInset));

    internal static Icon Close { get; } = Badge("关闭", Pink,
        Line(White, 4.2f, "M16 16L32 32M32 16L16 32"));

    internal static IReadOnlyList<Icon> All { get; } =
    [
        Badge("数据看板", Orange, Fill(White, "M10 10H22V24H10Z M26 10H38V19H26Z M26 23H38V38H26Z M10 28H22V38H10Z")),
        Badge("折线图", Purple, Line(White, 3.8f, "M11 32L20 22L27 27L37 15")),
        Badge("图表", Pink, Fill(White, Bars(10, 37, 9, 19, 14))),
        new("饼图", Face(Circle(24, 24, 20), Green,
            Fill(PaleGreen, "M24 24L31 5.265A20 20 0 0 1 41.32 34Z"),
            Line(Green.Edge, 1.25f, "M31 5.265L24 24L41.32 34"))),
        new("环形图", Face(Ring(24, 24, 21, 9), Blue,
            Fill(PaleBlue, "M24 3A21 21 0 0 1 38.85 38.85L30.36 30.36A9 9 0 0 0 24 15Z"),
            Line(Blue.Edge, 1.15f, "M24 3V15M30.36 30.36L38.85 38.85"))),
        Badge("面积图", Blue, Fill(White, "M11 37V26Q11 24 13 23L20 19L27 24L37 15V35Q37 37 35 37Z")),
        Badge("漏斗图", Orange, Fill(White, "M10 12H38L36 17Q35 19 33 19H15Q13 19 12 17Z M14 22H34L31 28H17Z M18 31H30L27 37H21Z")),
        Badge("动态", Green, Line(White, 3.5f, "M10 24H17L21 16L26 32L30 24H38")),
        Badge("上涨", Orange,
            Line(White, 3.7f, "M11 33L20 24L26 29L35 18"),
            Fill(White, "M29 18L37 16L36 24Z")),

        Badge("下跌", Orange,
            Line(White, 3.7f, "M11 15L20 24L26 19L35 30"),
            Fill(White, "M29 30L37 32L36 24Z")),
        new("仪表", Face(Svg("M3 32A21 21 0 0 1 45 32V37Q45 40 42 40H6Q3 40 3 37Z"), Blue,
            Fill(White, "M9 32A15 15 0 0 1 39 32Z"),
            Line(new Color(61, 100, 167), 2.1f, "M24 32L15 26"),
            Fill(new Color(61, 100, 167), Circle(24, 32, 3.3f)))),
        Badge("热力图", Purple, Fill(White, Grid(10, 10, 3, 3, 8, 2.2f, (0, 0), (1, 0), (0, 1), (1, 2), (2, 2))),
            Fill(PaleBlue, Grid(10, 10, 3, 3, 8, 2.2f, (2, 0), (1, 1))),
            Fill(Highlight, Grid(10, 10, 3, 3, 8, 2.2f, (2, 1), (0, 2)))),
        new("多人",
        [
            .. Face(Svg("M27 22H33Q44 22 45 34V37Q45 41 41 41H23V31Q23 25 27 22Z"), Green),
            .. Face(Circle(33, 11, 7.2f), Green),
            .. Face(Svg("M4 37Q4 27 15 27H20Q31 27 32 37V41Q32 44 29 44H7Q4 44 4 41Z"), Green,
                Fill(PaleGreen, "M6 37Q6 29 15 29H20Q29 29 30 37V41Q30 42 29 42H7Q6 42 6 41Z")),
            Fill(PaleGreen, Circle(17, 15, 7.5f)), Line(Green.Edge, 1.4f, Circle(17, 15, 7.5f)),
        ]),
        Disc("用户", Pink, Fill(White, Circle(24, 20, 7.5f)),
            Fill(White, "M9.5 37.4C13.9 29.5 34.1 29.5 38.5 37.4A20 20 0 0 1 9.5 37.4Z")),
        new("设置", Face(Gear(), Purple,
            Fill(White, Ring(24, 24, 10, 6.6f)))),
        new("调节",
        [
            .. Face(Rect(3, 10, 42, 10, 5), Orange), .. Face(Rect(3, 29, 42, 10, 5), Orange),
            Fill(PaleOrange, Circle(31, 15, 7)), Line(Orange.Edge, 1.3f, Circle(31, 15, 7)),
            Fill(PaleOrange, Circle(17, 34, 7)), Line(Orange.Edge, 1.3f, Circle(17, 34, 7)),
        ]),
        new("筛选", Face(Svg("M7 8H41Q43 8 42 11L30 25V38Q30 40 28 41L21 44Q19 44 19 42V25L6 11Q4 8 7 8Z"), Pink,
            Line(White, 2.7f, "M15 15H34M18 20H30"))),
        new("搜索",
        [
            .. Face(Rect(4, 4, 33, 38, 5), Orange, Fill(White, Rect(9, 10, 21, 2.7f, 1.3f)),
                Fill(White, Rect(9, 16, 15, 2.7f, 1.3f)), Fill(PaleOrange, Rect(6.5f, 22, 28, 17, 2))),
            Line(new Color(164, 72, 21), 3.7f, "M36 35L45 44"),
            Fill(new Color(255, 239, 222), Circle(33, 32, 7.5f)), Line(new Color(164, 72, 21), 3.2f, Circle(33, 32, 7.5f)),
        ]),

        new("通知",
        [
            Fill(new Color(168, 71, 16), Circle(24, 7, 3)), Fill(Orange.Edge, Circle(24, 38, 4)),
            .. Face(Svg("M9 33Q12 30 12 22V20A12 12 0 0 1 36 20V22Q36 30 39 33Z"), Orange),
            .. Face(Rect(5, 31, 38, 8, 4), Orange), Fill(Highlight, Rect(8, 32, 32, 2, 1)),
        ]),
        new("日历", Face(Rect(5, 7, 38, 36, 5), Pink,
            Fill(White, Rect(11, 18, 26, 19, 2)),
            Line(Pink.Edge, 4.4f, "M17 5V12M31 5V12"), Line(PalePink, 2.1f, "M17 5V12M31 5V12"),
            Line(Pink.Edge, 2.35f, "M16 25L20 23V32M25 23H32L27.5 32"))),
        new("收件箱", Face(Rect(3, 8, 42, 34, 5), Blue,
            Fill(White, Rect(11, 14, 26, 19, 2)), Fill(Blue.Edge, Rect(15, 18, 18, 2.5f, 1.25f)),
            Fill(PaleBlue, "M5 25H13Q15 25 16 27L18 31H30L32 27Q33 25 35 25H43V36Q43 40 39 40H9Q5 40 5 36Z"))),
        new("数据库", Face(Svg("M6 10H42V37C42 46 6 46 6 37Z"), Pink,
            Line(Pink.Edge, 1.5f, "M6 20C6 28 42 28 42 20M6 29C6 37 42 37 42 29"),
            Fill(PalePink, Oval(24, 10, 18, 5.4f)), Line(Pink.Edge, 1.5f, Oval(24, 10, 18, 5.4f)))),
        new("服务器",
        [
            .. Face(Rect(5, 4, 38, 13, 4), Pink), .. Face(Rect(5, 17.5f, 38, 13, 4), Pink),
            .. Face(Rect(5, 31, 38, 13, 4), Pink),
            Fill(White, Combine(Circle(12, 10.5f, 2), Circle(12, 24, 2), Circle(12, 37.5f, 2),
                Rect(17, 9.3f, 9, 2.4f, 1.2f), Rect(17, 22.8f, 9, 2.4f, 1.2f), Rect(17, 36.3f, 9, 2.4f, 1.2f))),
        ]),
        new("报表文件", Face(Svg("M10 4H29L41 16V41Q41 45 37 45H10Q6 45 6 41V8Q6 4 10 4Z"), Green,
            Fill(PaleGreen, "M29 5V14Q29 17 32 17H40Z"), Fill(White, Bars(12.5f, 37, 8, 16, 11)))),
        new("演示",
        [
            Line(Purple.Edge, 3.2f, "M24 31V44M24 33L16 43M24 33L32 43"),
            .. Face(Rect(6, 8, 36, 27, 3), Purple), Fill(White, Rect(10, 15, 28, 15, 1.5f)),
            Fill(Purple.Edge, Bars(13, 30, 4, 7, 10)),
            .. Face(Rect(3, 4, 42, 7, 3), Purple),
        ]),
        Badge("看板", Purple,
            Fill(White, "M9 10H18V21H9Z M21 10H30V21H21Z M33 10H40V21H33Z M9 25H18V36H9Z M33 25H40V36H33Z")),
        new("侧边栏", Face(Rect(3, 7, 42, 34, 5), Orange,
            Fill(PaleOrange, Rect(6, 10, 11, 28, 2)), Fill(White, Rect(21, 11, 19, 26, 1.5f)),
            Fill(Orange.Edge, Combine(Rect(8, 16, 6, 2.2f, 1), Rect(8, 22, 4, 2.2f, 1))))),

        Badge("列表视图", Pink,
            Fill(White, Combine(Grid(10, 12, 1, 3, 5, 4), Rect(19, 12, 18, 3.5f, 1.7f),
                Rect(19, 21, 14, 3.5f, 1.7f), Rect(19, 30, 17, 3.5f, 1.7f)))),
        Badge("网格", Purple, Fill(White, Grid(10, 10, 3, 3, 8, 2.2f))),
        Disc("下载", Blue,
            Fill(PaleBlue, "M5 30H43A20 20 0 0 1 5 30Z"),
            Line(White, 3.5f, "M24 8V24M17 18L24 25L31 18")),
        new("导出", Face(Rect(3, 7, 42, 34, 5), Blue,
            Fill(PaleBlue, "M5 29Q5 26 8 26H15L18 30H30L33 26H40Q43 26 43 29V36Q43 39 40 39H8Q5 39 5 36Z"),
            Fill(White, "M22 26V18H19L24 12L29 18H26V26Z"))),
        Disc("同步", Pink, Fill(PalePink, Circle(24, 24, 14)),
            Line(new Color(160, 43, 101), 3, "M17 20A8 8 0 0 1 30 18M31 28A8 8 0 0 1 18 30") with { Animated = true },
            Fill(new Color(160, 43, 101), "M29 14L33 20L26 21Z M19 34L15 28L22 27Z") with { Animated = true }),
        Disc("完成", Orange, Line(White, 4, "M15 24L22 31L34 17")),
        new("警告", Face(Svg("M21 7Q24 2 27 7L44 36Q47 41 41 42H7Q1 41 4 36Z"), Purple,
            Line(White, 3.4f, "M24 17V26"), Fill(White, Circle(24, 33, 2.3f)))),
        new("更多", Face(Rect(3, 12, 42, 24, 12), Green,
            Fill(White, Combine(Circle(14, 24, 3.3f), Circle(24, 24, 3.3f), Circle(34, 24, 3.3f))))),

        // 大背包的四个操作按原侧栏顺序排列。拿走与放入共用收纳盒轮廓。
        InventoryTransfer("全部拿走", Orange, PaleOrange, "M24 26V11M18.5 16.5L24 11L29.5 16.5"),
        InventoryTransfer("全部放入", Blue, PaleBlue, "M24 11V26M18.5 20.5L24 26L29.5 20.5"),
        Badge("快速堆叠", Green,
            Fill(PaleGreen, Combine(Rect(11, 26, 18, 5, 2), Rect(11, 33, 18, 5, 2))),
            Fill(White, Rect(11, 19, 18, 5, 2)),
            Line(White, 2.8f, "M34 11V29M30 25L34 29L38 25")),
        Badge("排序", Purple,
            Fill(White, Combine(Rect(11, 12, 17, 4, 2), Rect(11, 22, 13, 4, 2), Rect(11, 32, 9, 4, 2))),
            Line(White, 2.8f, "M34 13V35M30 31L34 35L38 31")),
        Clover,
    ];

    private static VectorPath RepeatCloverLeaf(VectorPath leaf)
    {
        var leaves = new VectorPathBuilder();
        var center = new Vector3(24, 24, 0);
        var fromLeaf = Matrix.CreateTranslation(-center);
        for (var i = 0; i < 4; i++)
        {
            var transform = fromLeaf * Matrix.CreateRotationZ(MathHelper.PiOver4 + i * MathHelper.PiOver2) *
                Matrix.CreateTranslation(center);
            leaves.AddPath(leaf, transform);
        }

        return leaves.Build();
    }

    private static Icon InventoryTransfer(string name, Palette palette, Color tray, string arrow) =>
        Badge(name, palette,
            Fill(tray, "M11 25H16L19 30H29L32 25H37V34Q37 38 33 38H15Q11 38 11 34Z"),
            Line(White, 1.5f, "M11 25H16L19 30H29L32 25H37"),
            Line(White, 3.3f, arrow));

    private static Icon Badge(string name, Palette palette, params Layer[] artwork) =>
        new(name, Face(Rect(4.5f, 4.5f, 39, 39, 5), palette,
            [Line(Highlight, 1, Rect(6.5f, 6.5f, 35, 35, 4)), .. artwork]));

    private static Icon Disc(string name, Palette palette, params Layer[] artwork) =>
        new(name, Face(Circle(24, 24, 20.5f), palette,
            [Line(Highlight, 1, Circle(24, 24, 18.8f)), .. artwork]));

    private static Layer[] Face(VectorPath path, Palette palette, params Layer[] artwork) =>
        [Fill(palette.Edge * 0.16f, path.Transform(Matrix.CreateTranslation(0, 2, 0))),
            new Layer(path, palette.Light, palette.Dark), Line(palette.Edge, 1.4f, path), .. artwork];

    private static Layer Fill(Color color, VectorPath path) => new(path, color, color);

    private static Layer Fill(Color color, string data) => Fill(color, Svg(data));

    private static Layer Line(Color color, float width, VectorPath path) => new(path, color, color, width);

    private static Layer Line(Color color, float width, string data) => Line(color, width, Svg(data));

    private static VectorPath Svg(string data) => VectorPath.ParseSvgPathData(data);

    private static VectorPath Circle(float x, float y, float radius) => Oval(x, y, radius, radius);

    private static VectorPath Oval(float x, float y, float radiusX, float radiusY) =>
        new VectorPathBuilder().AddEllipse(new Vector2(x, y), new Vector2(radiusX, radiusY)).Build();

    private static VectorPath Rect(float x, float y, float width, float height, float radius) =>
        new VectorPathBuilder().AddRoundRect(new Vector2(x, y), new Vector2(width, height), new Vector2(radius)).Build();

    private static VectorPath Ring(float x, float y, float outer, float inner) =>
        new VectorPathBuilder { FillRule = PathFillRule.EvenOdd }
            .AddCircle(new Vector2(x, y), outer).AddCircle(new Vector2(x, y), inner).Build();

    private static VectorPath Combine(params VectorPath[] paths)
    {
        var builder = new VectorPathBuilder();
        foreach (var path in paths) builder.AddPath(path);
        return builder.Build();
    }

    private static VectorPath Bars(float x, float bottom, params float[] heights)
    {
        var builder = new VectorPathBuilder();
        foreach (var height in heights)
        {
            builder.AddRoundRect(new Vector2(x, bottom - height), new Vector2(6, height), new Vector2(1.2f));
            x += 9;
        }
        return builder.Build();
    }

    private static VectorPath Grid(float x, float y, int columns, int rows, float size, float gap,
        params (int Column, int Row)[] cells)
    {
        var builder = new VectorPathBuilder();
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                if (cells.Length > 0 && !cells.Contains((column, row))) continue;
                builder.AddRoundRect(new Vector2(x + column * (size + gap), y + row * (size + gap)),
                    new Vector2(size), new Vector2(1.3f));
            }
        }
        return builder.Build();
    }

    private static VectorPath Gear()
    {
        var builder = new VectorPathBuilder();

        var center = new Vector2(24);
        for (var tooth = 0; tooth < 6; tooth++)
        {
            var angle = MathF.Tau * tooth / 6 - MathF.PI / 2;

            var rootBefore = GearPoint(angle - 0.41f, 16.5f);

            var tipBefore = GearPoint(angle - 0.23f, 21);

            var tipAfter = GearPoint(angle + 0.23f, 21);

            var rootAfter = GearPoint(angle + 0.41f, 16.5f);
            if (tooth == 0) builder.MoveTo(rootBefore);
            else builder.LineTo(rootBefore);
            builder.QuadTo(GearPoint(angle - 0.35f, 21), tipBefore)
                .QuadTo(center + (tipBefore + tipAfter - 2 * center) * 0.53f, tipAfter)
                .QuadTo(GearPoint(angle + 0.35f, 21), rootAfter);
        }
        return builder.Close().Build();
    }

    private static Vector2 GearPoint(float angle, float radius) =>
        new(24 + MathF.Cos(angle) * radius, 24 + MathF.Sin(angle) * radius);

    internal sealed record Icon(string Name, Layer[] Layers);
    internal readonly record struct Layer(VectorPath Path, Color Top, Color Bottom, float Stroke = 0, bool Animated = false);
    internal readonly record struct Palette(Color Light, Color Dark, Color Edge);
}
