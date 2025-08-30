namespace SilkyUIFramework;

public class FlexLine
{
    public List<UIView> Elements { get; }

    public FlexLine(UIView firstElement, float mainSize = 0, float crossSize = 0)
    {
        Elements = [firstElement];
        MainSize = mainSize;
        CrossSize = crossSize;
    }

    private FlexLine(List<UIView> elements) => Elements = elements;

    public float MainSize { get; set; }

    public float CrossSize { get; set; }

    public float CrossSpace { get; set; }

    public float GetFenceGap(float gap) => (Elements.Count - 1) * gap;

    public void UpdateMainSizeByRow(float gap)
    {
        MainSize = Elements.Sum(element => element.OuterBounds.Width) + GetFenceGap(gap);
    }

    public void UpdateMainSizeByColumn(float gap)
    {
        MainSize = Elements.Sum(element => element.OuterBounds.Height) + GetFenceGap(gap);
    }

    public static FlexLine CreateSingleRow(List<UIView> elements, float gap)
    {
        var line = new FlexLine(elements)
        {
            MainSize = elements.Count == 0 ? 0 : elements.Sum(element => element.OuterBounds.Width) + (elements.Count - 1) * gap,
            CrossSize = elements.Count == 0 ? 0 : elements.Max(element => element.OuterBounds.Height)
        };

        return line;
    }

    public static FlexLine CreateSingleColumn(List<UIView> elements, float gap)
    {
        var line = new FlexLine(elements)
        {
            MainSize = elements.Count == 0 ? 0 : elements.Sum(element => element.OuterBounds.Height) + (elements.Count - 1) * gap,
            CrossSize = elements.Count == 0 ? 0 : elements.Max(element => element.OuterBounds.Width)
        };

        return line;
    }
}