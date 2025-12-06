namespace SilkyUIFramework.Layout;

public class GridModule(UIElementGroup parent) : LayoutModule(parent)
{
    /// <summary>
    /// 行数据
    /// </summary>
    private TemplateDefinition[] _rows;

    /// <summary>
    /// 列数据
    /// </summary>
    private TemplateDefinition[] _columns;

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

    private float _rowsFraction = 0f;
    private float _columnsFraction = 0f;
    private float _rowsFenceGap = 0f;
    private float _columnsFenceGap = 0f;

    /// <summary>
    /// 统计行数列数
    /// </summary>
    public override void UpdateCacheStatus()
    {
        base.UpdateCacheStatus();

        var rows = _rows.AsSpan();
        var columns = _columns.AsSpan();

        _rowValues = new float[rows.Length];
        _columnValues = new float[columns.Length];
        
        _markers = new bool[rows.Length, columns.Length];

        var rowValues = _rowValues.AsSpan();
        var columnValues = _columnValues.AsSpan();

        // 更新 fr 和 gap 行和列的总和
        #region update fr and gap

        _rowsFraction = _rows.Where(row => row.TemplateType is TemplateType.Fraction).Sum(row => row.Value);
        _columnsFraction =
            _columns.Where(column => column.TemplateType is TemplateType.Fraction).Sum(column => column.Value);

        _columnsFenceGap = (rows.Length - 1) * Gap.Width;
        _rowsFenceGap = (rows.Length - 1) * Gap.Height;

        #endregion

        var availableSize = Parent.InnerBounds;

        // 初始化行和列的值
        // 自适应的时候只计算 pixels 行列的值
        // 固定大小时计算 percent + pixels 行列的值
        // fr 和 auto 行列值在 prepareChildren 之后计算
        #region row and column values

        if (FitWidth)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                columnValues[i] = columns[i].TemplateType switch
                {
                    TemplateType.Pixels => columns[i].Value,
                    { } => 0f
                };
            }
        }
        else
        {
            for (var i = 0; i < columns.Length; i++)
            {
                columnValues[i] = columns[i].TemplateType switch
                {
                    TemplateType.Percent => columns[i].Value * availableSize.Width,
                    TemplateType.Pixels => columns[i].Value,
                    { } => 0f
                };
            }
        }

        if (FitHeight)
        {
            for (var i = 0; i < rows.Length; i++)
            {
                rowValues[i] = rows[i].TemplateType switch
                {
                    TemplateType.Pixels => rows[i].Value,
                    { } => 0f
                };
            }
        }
        else
        {
            for (var i = 0; i < rows.Length; i++)
            {
                rowValues[i] = rows[i].TemplateType switch
                {
                    TemplateType.Percent => rows[i].Value * availableSize.Width,
                    TemplateType.Pixels => rows[i].Value,
                    { } => 0f
                };
            }
        }

        #endregion

        // 计算间隔
        _rowsFenceGap = rows.Length > 0 ? (rows.Length - 1) * Gap.Height : 0f;
        _columnsFenceGap = columns.Length > 0 ? (columns.Length - 1) * Gap.Width : 0f;
    }

    private void UpdateLocking(int rowStart, int rowEnd, int columnStart, int columnEnd)
    {
        rowStart--;
        columnStart--;
        if (rowStart < 0) rowStart = 0;
        if (columnStart < 0) columnStart = 0;
        if (rowEnd > _columns.Length) rowEnd = _columns.Length;
        if (columnEnd > _columns.Length) columnEnd = _columns.Length;

        for (var i = rowStart; i < rowEnd; i++)
        {
            for (var j = columnStart; j < columnEnd; j++)
            {
                _markers[i, j] = true;
            }
        }
    }

    private float GetColumnWidth(int start, int end)
    {
        start--;
        if (start < 0) start = 0;
        if (end > _columns.Length) end = _columns.Length;

        var width = 0f;
        for (var i = start; i < end; i++)
        {
            width = _columns[i].Value;
        }

        return width;
    }

    private float GetRowHeight(int start, int end)
    {
        start--;
        if (start < 0) start = 0;
        if (end > _rows.Length) end = _rows.Length;

        var height = 0f;
        for (var i = start; i < end; i++)
        {
            height = _rows[i].Value;
        }

        return height;
    }

    public sealed override void PreMeasure() { }

    public override void ModifyAvailableSize(UIView view, int index,
        ref float? availableWidth, ref float? availableHeight)
    {
        if (!view.GridArea) return;

        availableWidth = GetColumnWidth(view.ColumnStart, view.ColumnEnd);
        availableHeight = GetRowHeight(view.RowStart, view.RowEnd);
        UpdateLocking(view.RowStart, view.RowEnd, view.ColumnStart, view.ColumnEnd);
    }
}

public enum TemplateType
{
    Auto,
    Fraction,
    Pixels,
    Percent
}

public readonly struct TemplateDefinition(TemplateType templateType, float value = 0f)
{
    public TemplateType TemplateType { get; } = templateType;
    public float Value { get; } = value;

    public static IEnumerable<TemplateDefinition> Repeat(int quantity, TemplateType templateType, float value = 0f)
    {
        var units = new TemplateDefinition[quantity];
        for (var i = 0; i < units.Length; i++)
        {
            units[i] = new TemplateDefinition(templateType, value);
        }

        return units;
    }
}