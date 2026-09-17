namespace SilkyUIFramework.Elements.Components;

[XmlElementMapping("Header")]
public partial class UIHeader : SUIDraggableView
{
    public UIHeader() : base()
    {
        InitializeComponent();
        Title.UseDeathText();
    }
}