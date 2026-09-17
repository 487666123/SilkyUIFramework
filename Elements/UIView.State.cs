using SilkyUIFramework.StyleSystem;

namespace SilkyUIFramework.Elements;

public partial class UIView
{
    private UIElementState _state = UIElementState.Normal;

    public UIElementState State => _state;

    /// <summary>此元素专属的样式表，首次访问时创建并固定绑定到当前元素。</summary>
    public UIStyleSheet StyleSheet => field ??= new UIStyleSheet(this);

    public void SetState(UIElementState state)
    {
        if (_state == state) return;

        _state = state;
        ApplyCurrentStyle();
    }

    public void AddState(UIElementState state) => SetState(_state | state);

    public void RemoveState(UIElementState state) => SetState(_state & ~state);

    private void ApplyCurrentStyle() => StyleSheet?.ApplyStyle(_state);
}
