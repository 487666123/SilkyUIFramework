using SilkyUIFramework.Layout;

namespace SilkyUIFramework.UserInterfaces.GridTest;

[RegisterUI]
public partial class GridTestUI : BaseBody
{
    protected override void OnInitialize()
    {

#if DEBUG
        Enabled = true;
#endif

        InitializeComponent();

        Grid1.SetTemplateColumns([GridTrack.Fr(1f), GridTrack.Fr(1f)]);
        Grid1.SetTemplateRows([GridTrack.Fr(1f), GridTrack.Fr(1f)]);

        Grid2.SetTemplateColumns([GridTrack.Fr(2f), GridTrack.Fr(1f), GridTrack.Fr(1f)]);
        Grid2.SetTemplateRows([GridTrack.Fr(1f), GridTrack.Fr(1f), GridTrack.Pixels(48f)]);

        // 固定跨度示例的位置；Grid1 和 Grid4 单独测试自动放置。
        Big.RowSpan = GridSpan.At(0);
        Big.ColumnSpan = GridSpan.At(0, 2);
        Sidebar.RowSpan = GridSpan.At(0, 2);
        Sidebar.ColumnSpan = GridSpan.At(2);
        Small1.RowSpan = GridSpan.At(1);
        Small1.ColumnSpan = GridSpan.At(0);
        Small2.RowSpan = GridSpan.At(1);
        Small2.ColumnSpan = GridSpan.At(1);
        Footer.RowSpan = GridSpan.At(2);
        Footer.ColumnSpan = GridSpan.At(0, 3);

        Grid3.SetTemplateColumns([GridTrack.Fr(1f), GridTrack.Fr(1f)]);
        Grid3.SetTemplateRows([GridTrack.Fr(1f), GridTrack.Fr(1f)]);
        Box1.GridHorizontalAlignment = GridItemAlignment.Start;
        Box1.GridVerticalAlignment = GridItemAlignment.Start;
        Box2.GridHorizontalAlignment = GridItemAlignment.End;
        Box2.GridVerticalAlignment = GridItemAlignment.Start;
        Box3.GridHorizontalAlignment = GridItemAlignment.Start;
        Box3.GridVerticalAlignment = GridItemAlignment.End;
        Box4.GridHorizontalAlignment = GridItemAlignment.Center;
        Box4.GridVerticalAlignment = GridItemAlignment.Center;

        // 不声明行模板，由三组标签和值撑开隐式 Auto 行。
        Grid4.SetTemplateColumns([GridTrack.Auto, GridTrack.Fr(1f)]);

        DragPanel.ControlTarget = this;
        TestScroll.ScrollBar.BorderRadius = Vector4.Zero;
        TestScroll.ScrollBar.Thumb.BorderRadius = Vector4.Zero;
        TestScroll.ScrollBar.BackgroundColor = Color.White * 0.25f;
        TestScroll.ScrollBar.Thumb.BarColor = (Color.White * 0.25f, Color.White * 0.5f);
    }
}
