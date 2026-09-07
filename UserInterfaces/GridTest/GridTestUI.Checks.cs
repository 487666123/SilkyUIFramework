namespace SilkyUIFramework.UserInterfaces.GridTest;

public partial class GridTestUI
{
    private const float GeometryTolerance = 0.25f;
    private static readonly Color PassedColor = new(114, 216, 171);
    private static readonly Color FailedColor = new(238, 137, 143);

    public override void UpdateLayout()
    {
        var layoutChanged = LayoutIsDirty;
        base.UpdateLayout();
        if (!layoutChanged) return;

        // 使用布局完成后的尺寸和局部偏移，检查结果不受窗口拖动、滚动影响。
        var passed = 0;
        if (ShowResult(Result1, CheckEqualTracks())) passed++;
        if (ShowResult(Result2, CheckSpanningTracks())) passed++;
        if (ShowResult(Result3, CheckItemAlignment())) passed++;
        if (ShowResult(Result4, CheckImplicitRows())) passed++;

        CheckSummary.Text = $"几何检查：{passed} / 4 通过";
        CheckSummary.TextColor = passed == 4 ? PassedColor : FailedColor;
        MeasuredSize.Text = $"{TestScroll.Mask.InnerBounds.Width:0} x {TestScroll.Mask.InnerBounds.Height:0} px";
    }

    private bool CheckEqualTracks()
    {
        var width = (Grid1.InnerBounds.Width - Grid1.Gap.Width) / 2f;
        var height = (Grid1.InnerBounds.Height - Grid1.Gap.Height) / 2f;
        var nextX = width + Grid1.Gap.Width;
        var nextY = height + Grid1.Gap.Height;

        return HasContentWidth(Grid1) &&
               Matches(Item1, 0f, 0f, width, height) &&
               Matches(Item2, nextX, 0f, width, height) &&
               Matches(Item3, 0f, nextY, width, height) &&
               Matches(Item4, nextX, nextY, width, height);
    }

    private bool CheckSpanningTracks()
    {
        var gapX = Grid2.Gap.Width;
        var gapY = Grid2.Gap.Height;
        var unitWidth = (Grid2.InnerBounds.Width - 2f * gapX) / 4f;
        var rowHeight = (Grid2.InnerBounds.Height - 2f * gapY - 48f) / 2f;
        var nextY = rowHeight + gapY;

        return HasContentWidth(Grid2) &&
               Matches(Big, 0f, 0f, 3f * unitWidth + gapX, rowHeight) &&
               Matches(Sidebar, 3f * unitWidth + 2f * gapX, 0f, unitWidth, 2f * rowHeight + gapY) &&
               Matches(Small1, 0f, nextY, 2f * unitWidth, rowHeight) &&
               Matches(Small2, 2f * unitWidth + gapX, nextY, unitWidth, rowHeight) &&
               Matches(Footer, 0f, 2f * nextY, Grid2.InnerBounds.Width, 48f);
    }

    private bool CheckItemAlignment()
    {
        var width = Grid3.InnerBounds.Width;
        var height = Grid3.InnerBounds.Height;
        var cellWidth = (width - Grid3.Gap.Width) / 2f;
        var cellHeight = (height - Grid3.Gap.Height) / 2f;
        var centerX = cellWidth + Grid3.Gap.Width + (cellWidth - 60f) / 2f;
        var centerY = cellHeight + Grid3.Gap.Height + (cellHeight - 60f) / 2f;

        return HasContentWidth(Grid3) && cellWidth >= 60f && cellHeight >= 60f &&
               Matches(Box1, 0f, 0f, 60f, 60f) &&
               Matches(Box2, width - 60f, 0f, 60f, 60f) &&
               Matches(Box3, 0f, height - 60f, 60f, 60f) &&
               Matches(Box4, centerX, centerY, 60f, 60f);
    }

    private bool CheckImplicitRows()
    {
        var labelWidth = Math.Max(Label1.OuterBounds.Width,
            Math.Max(Label2.OuterBounds.Width, Label3.OuterBounds.Width));
        var valueX = labelWidth + Grid4.Gap.Width;
        var valueWidth = Grid4.InnerBounds.Width - valueX;
        var firstHeight = Math.Max(Label1.OuterBounds.Height, 36f);
        var secondHeight = Math.Max(Label2.OuterBounds.Height, 36f);
        var thirdHeight = Math.Max(Label3.OuterBounds.Height, 36f);
        var secondY = firstHeight + Grid4.Gap.Height;
        var thirdY = secondY + secondHeight + Grid4.Gap.Height;

        return HasContentWidth(Grid4) && Grid4.TemplateRows.Count == 0 &&
               Near(Grid4.InnerBounds.Height, thirdY + thirdHeight) &&
               MatchesFormRow(Label1, Input1, 0f, firstHeight, labelWidth, valueX, valueWidth) &&
               MatchesFormRow(Label2, Input2, secondY, secondHeight, labelWidth, valueX, valueWidth) &&
               MatchesFormRow(Label3, Input3, thirdY, thirdHeight, labelWidth, valueX, valueWidth);
    }

    private bool HasContentWidth(UIElementGroup grid)
    {
        return grid.InnerBounds.Width > 0f &&
               Near(grid.OuterBounds.Width, TestScroll.Container.InnerBounds.Width);
    }

    private static bool MatchesFormRow(UIView label, UIView value, float y, float height,
        float labelWidth, float valueX, float valueWidth)
    {
        return Matches(label, labelWidth - label.OuterBounds.Width,
                   y + (height - label.OuterBounds.Height) / 2f,
                   label.OuterBounds.Width, label.OuterBounds.Height) &&
               Matches(value, valueX, y + (height - 36f) / 2f, valueWidth, 36f);
    }

    private static bool Matches(UIView item, float x, float y, float width, float height)
    {
        return width > 0f && height > 0f &&
               Near(item.LayoutOffset.X, x) && Near(item.LayoutOffset.Y, y) &&
               Near(item.OuterBounds.Width, width) && Near(item.OuterBounds.Height, height);
    }

    private static bool Near(float actual, float expected) => Math.Abs(actual - expected) <= GeometryTolerance;

    private static bool ShowResult(UITextView label, bool passed)
    {
        label.Text = passed ? "通过" : "异常";
        label.TextColor = passed ? PassedColor : FailedColor;
        return passed;
    }
}
