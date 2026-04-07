using System.Windows.Input;

namespace SilkyUIFramework.Elements;

[XmlElementMapping("ButtonBase")]
public class ButtonBase : UIElementGroup
{
    public ICommand Command { get; set; }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);

        if (Command == null) return;
        if (Command.CanExecute(null)) Command.Execute(null);
    }
}
