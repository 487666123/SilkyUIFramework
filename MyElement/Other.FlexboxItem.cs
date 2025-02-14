namespace SilkyUIFramework;

public partial class Other : IFlexboxItem
{
    public float FlexGrow
    {
        get => _flexGrow;
        set
        {
            if (value == _flexGrow) return;
            _flexGrow = value;
            BubbleMarkerDirty();
        }
    }
    private float _flexGrow;

    public float FlexShrink
    {
        get => _flexShrink;
        set
        {
            if (value == _flexShrink) return;
            _flexShrink = value;
            BubbleMarkerDirty();
        }
    }
    private float _flexShrink;
}