using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

public partial class UIElementGroup
{
    #region Grid Properties

    private GridTrack[] _templateRows = [];

    public IReadOnlyList<GridTrack> TemplateRows => _templateRows;

    public GridDirection GridDirection
    {
        get; set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public void SetTemplateRows(GridTrack[] rows)
    {
        if (_templateRows == rows) return;
        _templateRows = rows is null ? [] : [.. rows];
        MarkLayoutDirty();
    }

    private GridTrack[] _templateColumns = [];

    public IReadOnlyList<GridTrack> TemplateColumns => _templateColumns;

    public void SetTemplateColumns(GridTrack[] columns)
    {
        if (_templateColumns == columns) return;
        _templateColumns = columns is null ? [] : [.. columns];
        MarkLayoutDirty();
    }

    public GridItemAlignment GridItemsHorizontalAlignment
    {
        get; set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    } = GridItemAlignment.Stretch;

    public GridItemAlignment GridItemsVerticalAlignment
    {
        get; set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    } = GridItemAlignment.Stretch;

    #endregion

    public FlexDirection FlexDirection
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public bool FlexWrap
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public MainAlignment MainAlignment
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public CrossAlignment CrossAlignment
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public CrossContentAlignment CrossContentAlignment
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    } = CrossContentAlignment.Stretch;
}
