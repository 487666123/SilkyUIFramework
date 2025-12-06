namespace SilkyUIFramework;

public class MouseStatus
{
    private readonly bool[] _buttons = new bool[3];

    public bool this[MouseButtonType button]
    {
        get => _buttons[(int)button];
        set => _buttons[(int)button] = value;
    }

    public void SetState(MouseStatus status)
    {
        Array.Copy(status._buttons, _buttons, _buttons.Length);
    }

    public void SetState(bool left, bool middle, bool right)
    {
        _buttons[0] = left;
        _buttons[1] = middle;
        _buttons[2] = right;
    }
}