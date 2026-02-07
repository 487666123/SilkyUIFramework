using Microsoft.Xna.Framework.Input;
using ReLogic.Localization.IME;
using ReLogic.OS;

namespace SilkyUIFramework;

[Service]
public class SilkyUIInputState(SilkyUIRenderSystem renderSystem)
{
    private readonly SilkyUIRenderSystem _renderSystem = renderSystem;
    public static MouseButtonType[] MouseButtons { get; } = [.. Enum.GetValues(typeof(MouseButtonType)).Cast<MouseButtonType>()];
    public static Vector2 MousePosition => new(Main.mouseX, Main.mouseY);

    public MouseStatus CurrentMouseStatus { get; } = new();
    public MouseStatus PreviousMouseStatus { get; } = new();

    public UIView HoveredElement { get; private set; }
    public UIView PreviousHoveredElement { get; private set; }

    private SilkyUIGroup _silkyUIGroup;
    private SilkyUI _silkyUI;

    private Dictionary<MouseButtonType, UIView> PressedElements { get; } = [];

    public UIView FocusedElement { get; private set; }

    internal void UpdateMouseStatus()
    {
        PreviousMouseStatus.SetState(CurrentMouseStatus);
        CurrentMouseStatus.SetState(Main.mouseLeft, Main.mouseMiddle, Main.mouseRight);
    }

    internal void UpdateScrollEvent()
    {
        var target = HoveredElement;

        if (target != null && PlayerInput.ScrollWheelDeltaForUI != 0)
        {
            RuntimeSafeHelper.SafeInvoke(() => target.OnMouseWheel(new(target, MousePosition, PlayerInput.ScrollWheelDeltaForUI)));
        }

        if (FocusedElement is { OccupyPlayerInput: true } inputElement)
            Main.CurrentInputTextTakerOverride = inputElement;
    }

    internal void UpdateMouseEvent()
    {
        UpdateFocusedElement();

        foreach (var buttonType in MouseButtons)
        {
            if (CurrentMouseStatus[buttonType])
            {
                if (PreviousMouseStatus[buttonType]) continue;
                HandleMouseDown(buttonType);
                UpdateFocusElement(HoveredElement);
            }
            else
            {
                if (!PreviousMouseStatus[buttonType]) continue;
                HandleMouseUp(buttonType);
            }
        }
    }

    internal void UpdateHoverTarget()
    {
        _renderSystem.GetHoverTarget(out _silkyUIGroup, out _silkyUI, out var element);

        PreviousHoveredElement = HoveredElement;

        if (HoveredElement == element) return;
        HoveredElement = element;

        if (PreviousHoveredElement != null)
            RuntimeSafeHelper.SafeInvoke(() => PreviousHoveredElement.OnMouseLeave(new(PreviousHoveredElement, MousePosition)));
        if (HoveredElement != null)
            RuntimeSafeHelper.SafeInvoke(() => HoveredElement.OnMouseEnter(new(HoveredElement, MousePosition)));
    }

    internal void UpdateFocusedElement()
    {
        if (FocusedElement?.SilkyUI?.RootNode is { Enabled: true, IsInteractable: true }) return;

        var previousFocusTarget = FocusedElement;
        FocusedElement = null;

        if (previousFocusTarget != null)
            RuntimeSafeHelper.SafeInvoke(() => previousFocusTarget.OnLostFocus(new(previousFocusTarget, MousePosition)));
    }

    internal void UpdateFocusElement(UIView hoveredElement)
    {
        if (hoveredElement == FocusedElement) return;

        var previousFocusTarget = FocusedElement;
        FocusedElement = hoveredElement;

        if (previousFocusTarget != null)
            RuntimeSafeHelper.SafeInvoke(() => previousFocusTarget.OnLostFocus(new(FocusedElement, MousePosition)));
        if (FocusedElement != null)
            RuntimeSafeHelper.SafeInvoke(() => FocusedElement.OnGotFocus(new(FocusedElement, MousePosition)));
    }

    private void HandleMouseDown(MouseButtonType buttonType)
    {
        var hoverEdElement = HoveredElement;
        PressedElements[buttonType] = hoverEdElement;
        if (hoverEdElement == null) return;

        RuntimeSafeHelper.SafeInvoke(delegate
        {
            if (_silkyUIGroup != null && _silkyUI != null)
            {
                _silkyUIGroup.MoveToTop(_silkyUI);
            }

            switch (buttonType)
            {
                case MouseButtonType.Left:
                    hoverEdElement.OnLeftMouseDown(new(hoverEdElement, MousePosition));
                    break;
                case MouseButtonType.Middle:
                    hoverEdElement.OnMiddleMouseDown(new(hoverEdElement, MousePosition));
                    break;
                case MouseButtonType.Right:
                    hoverEdElement.OnRightMouseDown(new(hoverEdElement, MousePosition));
                    break;
                default: return;
            }
        });
    }

    private void HandleMouseUp(MouseButtonType buttonType)
    {
        var mouseElement = PressedElements[buttonType];
        if (mouseElement == null) return;

        PressedElements[buttonType] = null;

        RuntimeSafeHelper.SafeInvoke(delegate
        {
            switch (buttonType)
            {
                case MouseButtonType.Left:
                    mouseElement?.OnLeftMouseUp(new UIMouseEvent(mouseElement, MousePosition));
                    break;
                case MouseButtonType.Middle:
                    mouseElement?.OnMiddleMouseUp(new UIMouseEvent(mouseElement, MousePosition));
                    break;
                case MouseButtonType.Right:
                    mouseElement?.OnRightMouseUp(new UIMouseEvent(mouseElement, MousePosition));
                    break;
                default: return;
            }
        });

        if (mouseElement != HoveredElement) return;

        RuntimeSafeHelper.SafeInvoke(delegate
        {
            switch (buttonType)
            {
                case MouseButtonType.Left:
                    mouseElement?.OnLeftMouseClick(new UIMouseEvent(mouseElement, MousePosition));
                    break;
                case MouseButtonType.Middle:
                    mouseElement?.OnMiddleMouseClick(new UIMouseEvent(mouseElement, MousePosition));
                    break;
                case MouseButtonType.Right:
                    mouseElement?.OnRightMouseClick(new UIMouseEvent(mouseElement, MousePosition));
                    break;
            }
        });
    }

    internal void HandleIME()
    {
        if (FocusedElement is not { OccupyPlayerInput: true }) return;

        PlayerInput.WritingText = true;
        Main.instance.HandleIME();
    }

    internal void HandleInput(SpriteBatch spriteBatch)
    {
        if (FocusedElement is not { OccupyPlayerInput: true }) return;

        if (!Main.hasFocus) return; // 焦点不在游戏

        Main.oldInputText = Main.inputText;
        Main.inputText = Keyboard.GetState();

        spriteBatch.ReBegin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

        var imeService = Platform.Get<IImeService>();
        Main.instance.DrawWindowsIMEPanel(FocusedElement.InputMethodPosition);

        FocusedElement.HandlePlayerInput(imeService.CandidateCount > 0);

        spriteBatch.ReBegin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
    }
}
