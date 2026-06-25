namespace SilkyUIFramework.Layout.Grid;

internal readonly struct FlowRect
{
    public FlowRect(int majorStart, int minorStart, int majorEnd, int minorEnd)
    {
        MajorStart = Math.Max(0, majorStart);
        MinorStart = Math.Max(0, minorStart);
        MajorEnd = Math.Max(MajorStart + 1, majorEnd);
        MinorEnd = Math.Max(MinorStart + 1, minorEnd);
    }

    public int MajorStart { get; }

    public int MinorStart { get; }

    public int MajorEnd { get; }

    public int MinorEnd { get; }
}
