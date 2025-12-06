namespace SilkyUIFramework.Elements;

public class SUIDividingLine : UIView
{
    private SUIDividingLine(Color bgColor)
    {
        //ZIndex = 1f;
        BackgroundColor = bgColor;
        Selectable = false;
    }

    public static SUIDividingLine Horizontal(Color? bgColor = null)
    {
        var line = new SUIDividingLine(bgColor ?? new Color(18, 18, 38) * 0.75f);
        line.SetSize(0f, 2f, 1f);
        return line;
    }

    public static SUIDividingLine Vertical(Color? bgColor = null)
    {
        var line = new SUIDividingLine(bgColor ?? new Color(18, 18, 38) * 0.75f);
        line.SetSize(2f, 0f, 0f, 1f);
        return line;
    }
}

[XmlElementMapping("HR")]
public class HorizontalRule : UIView
{
    public HorizontalRule()
    {
        Selectable = false;
        BackgroundColor = SUIColor.Border;

        Width = new Dimension(0f, 1f);
        Height = new Dimension(2f);
    }
}

[XmlElementMapping("VR")]
public class VerticalRule : UIView
{
    public VerticalRule()
    {
        Selectable = false;
        BackgroundColor = SUIColor.Border;

        Height = new Dimension(0f, 1f);
        Width = new Dimension(2f);
    }
}