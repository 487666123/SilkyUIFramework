namespace SilkyUIFramework.Elements;

public partial class UIView
{
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

    public bool GridArea
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (Parent?.LayoutType is not LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    }

    public void SetRow(int start, int end)
    {
        RowStart = start;
        RowEnd = end;
    }

    public void SetColumn(int start, int end)
    {
        ColumnStart = start;
        ColumnEnd = end;
    }

    public int RowStart
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (!GridArea) return;
            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    }

    public int RowEnd
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (!GridArea) return;
            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    }

    public int ColumnStart
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (!GridArea) return;
            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    }

    public int ColumnEnd
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (!GridArea) return;
            if (Parent == null) return;
            if (Parent.LayoutType != LayoutType.Grid) return;
            MarkLayoutDirty();
        }
    }
}