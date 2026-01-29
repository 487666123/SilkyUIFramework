using Microsoft.CodeAnalysis;

namespace SilkyUIFramework.Layout;

public enum AutoFlow { Row, Column }

public class GridModule(UIElementGroup parent) : LayoutModule(parent)
{
    /// <summary>
    /// 元素在表格中实际位置
    /// </summary>
    private struct Location(int x, int y, int width, int height)
    {
        public int X { get; set => field = Math.Max(0, value); } = x;
        public int Y { get; set => field = Math.Max(0, value); } = y;
        public int Width { get; set => field = Math.Max(1, value); } = width;
        public int Height { get; set => field = Math.Max(1, value); } = height;

        public readonly int Right => X + Width;
        public readonly int Bottom => Y + Height;
    }

    /// <summary>
    /// 行值
    /// </summary>
    private float[] _rowValues;

    /// <summary>
    /// 列值
    /// </summary>
    private float[] _columnValues;

    /// <summary>
    /// 格子标记
    /// </summary>
    private bool[,] _markers;

    private float _rowsFenceGap;
    private float _columnsFenceGap;

    /// <summary>
    /// 更新大小固定的行的大小
    /// </summary>
    private void UpdateFixedRows(float width)
    {
        var rows = Parent.TemplateRows;

        if (Parent.FitHeight)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                _rowValues[i] = rows[i].TemplateType switch
                {
                    TemplateType.Pixels => rows[i].Value,
                    { } => 0f
                };
            }
        }
        else
        {
            for (var i = 0; i < rows.Count; i++)
            {
                _rowValues[i] = rows[i].TemplateType switch
                {
                    TemplateType.Percent => rows[i].Value * width,
                    TemplateType.Pixels => rows[i].Value,
                    { } => 0f
                };
            }
        }
    }

    /// <summary>
    /// 更新大小固定的列的大小
    /// </summary>
    private void UpdateFixedColumns(float height)
    {
        var columns = Parent.TemplateColumns;

        if (Parent.FitWidth)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                _columnValues[i] = columns[i].TemplateType switch
                {
                    TemplateType.Pixels => columns[i].Value,
                    { } => 0f
                };
            }
        }
        else
        {
            for (var i = 0; i < columns.Count; i++)
            {
                _columnValues[i] = columns[i].TemplateType switch
                {
                    TemplateType.Percent => columns[i].Value * height,
                    TemplateType.Pixels => columns[i].Value,
                    { } => 0f
                };
            }
        }
    }

    // 记录元素位置
    private Location[] _locations;

    public override void PrepareData()
    {
        var list = Parent.LayoutChildren;
        _locations = new Location[list.Count];

        var rows = Parent.TemplateRows;
        var columns = Parent.TemplateColumns;

        // 行列大小默认都是 0
        _rowValues = new float[rows.Count];
        _columnValues = new float[columns.Count];

        // 标记元素
        _markers = new bool[rows.Count, columns.Count];

        // 计算行列 fr 总数
        //_rowsTotalFraction = rows.Where(row => row.TemplateType is TemplateType.Fraction).Sum(row => row.Value);
        //_columnsTotalFraction = columns.Where(column => column.TemplateType is TemplateType.Fraction).Sum(column => column.Value);

        _rowsFenceGap = (rows.Count - 1) * Parent.Gap.Height;
        _columnsFenceGap = (columns.Count - 1) * Parent.Gap.Width;

        UpdateFixedRows(0);
        UpdateFixedColumns(0);

        // 横纵皆定义, marker!
        for (int i = 0; i < list.Count; i++)
        {
            var el = list[i];
            ref var location = ref _locations[i];

            location = new Location(
                el.ColumnSpan.Start.Value, el.RowSpan.Start.Value,
                el.ColumnSpan.Size, el.RowSpan.Size);

            if (el.RowSpan.Start.HasValue && el.ColumnSpan.Start.HasValue)
            {
                var bottom = Math.Min(location.Bottom, rows.Count);
                var right = Math.Min(location.Right, columns.Count);

                for (var j = location.Y; j < bottom; j++)
                {
                    for (int k = location.X; k < right; k++)
                    {
                        // Success!
                        _markers[j, k] = true;
                    }
                }
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            var el = list[i];
            ref var location = ref _locations[i];

            if (el.RowSpan.Start.HasValue)
            {
                var right = Math.Min(location.Right, columns.Count);

                for (int k = location.X; k < right; k++)
                {
                    // Success!
                    _markers[el.RowSpan.Start.Value, k] = true;
                }
            }
            else if (el.ColumnSpan.Start.HasValue)
            {

            }
        }
    }

    public override void MeasureChildren() { }

    public sealed override void Measure() { }

    //public override void ModifyAvailableSize(UIView view, int index,
    //    ref float availableWidth, ref float availableHeight)
    //{
    //    if (!view.GridArea) return;

    //    availableWidth = GetColumnWidth(view.ColumnStart, view.ColumnEnd);
    //    availableHeight = GetRowHeight(view.RowStart, view.RowEnd);
    //    UpdateLocking(view.RowStart, view.RowEnd, view.ColumnStart, view.ColumnEnd);
    //}
}

public enum TemplateType { Auto, Fraction, Pixels, Percent }

/// <summary>
/// 横列的模板定义
/// </summary>
public readonly struct GridTrack(TemplateType templateType, float value = 0f) : IEquatable<GridTrack>
{
    public TemplateType TemplateType { get; } = templateType;
    public float Value { get; } = value;

    public static GridTrack[] Repeat(int quantity, TemplateType templateType, float value = 0f)
    {
        var units = new GridTrack[quantity];
        for (var i = 0; i < units.Length; i++)
        {
            units[i] = new GridTrack(templateType, value);
        }

        return units;
    }

    public static bool operator ==(GridTrack left, GridTrack right)
    {
        if (left.TemplateType == right.TemplateType && left.Value == right.Value) return true;

        return false;
    }
    public static bool operator !=(GridTrack left, GridTrack right) => !(left == right);

    public bool Equals(GridTrack other) => this == other;

    public override bool Equals(object obj) => obj is GridTrack other && this == other;

    public override int GetHashCode() => HashCode.Combine(TemplateType, Value);
}

/// <summary>
/// 元素在 Grid 中的位置和大小
/// </summary>
public readonly struct GridSpan : IEquatable<GridSpan>
{
    public GridSpan() => Size = 1;

    public readonly int? Start { get; }

    public readonly int Size { get; }

    public static bool operator ==(GridSpan left, GridSpan right)
    {
        if (left.Start == right.Start && left.Size == right.Size) return true;

        return false;
    }

    public static bool operator !=(GridSpan left, GridSpan right) => !(left == right);

    public bool Equals(GridSpan other) => this == other;

    public override bool Equals(object obj) => obj is GridSpan other && this == other;

    public override int GetHashCode() => HashCode.Combine(Start, Size);
}