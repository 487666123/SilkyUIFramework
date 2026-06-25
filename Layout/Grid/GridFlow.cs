namespace SilkyUIFramework.Layout.Grid;

internal readonly struct GridFlow(GridFlowDirection major, GridFlowDirection minor)
{
    public GridFlowDirection Major { get; } = major;

    public GridFlowDirection Minor { get; } = minor;

    public static GridFlow From(GridFlowDirection direction)
    {
        return direction switch
        {
            GridFlowDirection.Column => new GridFlow(GridFlowDirection.Column, GridFlowDirection.Row),
            _ => new GridFlow(GridFlowDirection.Row, GridFlowDirection.Column)
        };
    }
}
