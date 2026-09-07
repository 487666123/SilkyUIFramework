using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

public partial class UIView
{
    public GridSpan RowSpan
    {
        get; set
        {
            if (field == value) return;
            field = value;

            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    } = GridSpan.Auto;

    public GridSpan ColumnSpan
    {
        get; set
        {
            if (field == value) return;
            field = value;

            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    } = GridSpan.Auto;

    public GridItemAlignment GridHorizontalAlignment
    {
        get; set
        {
            if (field == value) return;
            field = value;

            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    } = GridItemAlignment.Inherit;

    public GridItemAlignment GridVerticalAlignment
    {
        get; set
        {
            if (field == value) return;
            field = value;

            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    } = GridItemAlignment.Inherit;

    /// <summary> 弹性项目的增长因子 </summary>
    public float FlexGrow
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Flexbox) return;
            MarkLayoutDirty();
        }
    }

    /// <summary> 弹性项目的收缩因子 </summary>
    public float FlexShrink
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Flexbox) return;
            MarkLayoutDirty();
        }
    }
}
