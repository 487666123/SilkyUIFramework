using Microsoft.Xna.Framework.Input;
using ReLogic.Localization.IME;
using ReLogic.OS;

namespace SilkyUIFramework;

/// <summary>
/// UI 输入状态管理器。
/// 负责在每帧中维护鼠标状态、悬停目标、焦点目标，并分发鼠标与输入法相关事件。
/// </summary>
/// <param name="renderSystem">用于命中测试的渲染系统实例。</param>
[Service]
public class SilkyUIInputState(SilkyUIRenderSystem renderSystem)
{
    /// <summary>
    /// 渲染系统引用，用于查询鼠标命中的 UI 元素。
    /// </summary>
    private readonly SilkyUIRenderSystem _renderSystem = renderSystem;

    /// <summary>
    /// 支持处理的鼠标按键集合。
    /// </summary>
    public static MouseButtonType[] MouseButtons { get; } = [.. Enum.GetValues(typeof(MouseButtonType)).Cast<MouseButtonType>()];

    /// <summary>
    /// 当前帧鼠标屏幕坐标。
    /// </summary>
    public static Vector2 MousePosition => new(Main.mouseX, Main.mouseY);

    /// <summary>
    /// 当前帧鼠标按键状态。
    /// </summary>
    public MouseStatus CurrentMouseStatus { get; } = new();

    /// <summary>
    /// 上一帧鼠标按键状态。
    /// </summary>
    public MouseStatus PreviousMouseStatus { get; } = new();

    /// <summary>
    /// 当前鼠标悬停元素。
    /// </summary>
    public UIView HoveredElement { get; private set; }

    /// <summary>
    /// 上一帧鼠标悬停元素。
    /// </summary>
    public UIView PreviousHoveredElement { get; private set; }

    /// <summary>
    /// 悬停元素所属 UI 栈（用于点击时置顶）。
    /// </summary>
    private SilkyUIStack _hoveredStack;

    /// <summary>
    /// 悬停元素所属 UI 实例（用于点击时置顶）。
    /// </summary>
    private SilkyUI _hoveredSilkyUI;

    /// <summary>
    /// 每个鼠标按键在按下瞬间对应的元素。
    /// 用于抬起时判断事件目标与 Click 判定。
    /// </summary>
    private Dictionary<MouseButtonType, UIView> PressedElements { get; } = [];

    /// <summary>
    /// 当前获得焦点的元素。
    /// </summary>
    public UIView FocusedElement { get; private set; }

    /// <summary>
    /// 刷新鼠标状态缓存：将当前状态写入上一帧，再采样本帧状态。
    /// </summary>
    internal void UpdateMouseStatus()
    {
        PreviousMouseStatus.SetState(CurrentMouseStatus);
        CurrentMouseStatus.SetState(Main.mouseLeft, Main.mouseMiddle, Main.mouseRight);
    }

    /// <summary>
    /// 分发滚轮事件，并在需要时将焦点元素注册为输入接管者。
    /// </summary>
    internal void UpdateScrollEvent()
    {
        var target = HoveredElement;

        if (PlayerInput.ScrollWheelDeltaForUI != 0)
        {
            target?.OnMouseWheel(new(target, MousePosition, PlayerInput.ScrollWheelDeltaForUI));
        }

        if (FocusedElement is { OccupyPlayerInput: true } inputElement)
            Main.CurrentInputTextTakerOverride = inputElement;
    }

    /// <summary>
    /// 根据按键边沿变化分发 MouseDown/MouseUp/Click，并更新焦点。
    /// </summary>
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

    /// <summary>
    /// 更新当前悬停目标，并在目标变化时触发 MouseLeave/MouseEnter。
    /// </summary>
    internal void UpdateHoverTarget()
    {
        _renderSystem.GetHoverTarget(out _hoveredStack, out _hoveredSilkyUI, out var element);

        PreviousHoveredElement = HoveredElement;

        if (HoveredElement == element) return;
        HoveredElement = element;

        PreviousHoveredElement?.OnMouseLeave(new(PreviousHoveredElement, MousePosition));
        HoveredElement?.OnMouseEnter(new(HoveredElement, MousePosition));
    }

    /// <summary>
    /// 校验当前焦点是否仍然有效；失效时清空并触发 LostFocus。
    /// </summary>
    internal void UpdateFocusedElement()
    {
        if (FocusedElement?.SilkyUI?.RootNode is { Enabled: true, IsInteractable: true }) return;

        var previousFocusTarget = FocusedElement;
        FocusedElement = null;

        previousFocusTarget?.OnLostFocus(new(previousFocusTarget, MousePosition));
    }

    /// <summary>
    /// 将焦点切换到指定悬停元素，并触发 LostFocus/GotFocus。
    /// </summary>
    /// <param name="hoveredElement">候选焦点元素。</param>
    internal void UpdateFocusElement(UIView hoveredElement)
    {
        if (hoveredElement == FocusedElement) return;

        var previousFocusTarget = FocusedElement;
        FocusedElement = hoveredElement;

        previousFocusTarget?.OnLostFocus(new(previousFocusTarget, MousePosition));
        FocusedElement?.OnGotFocus(new(FocusedElement, MousePosition));
    }

    /// <summary>
    /// 处理按键按下：
    /// 记录按下目标，必要时置顶 UI，并向目标分发 MouseDown 事件。
    /// </summary>
    /// <param name="buttonType">触发的鼠标按键。</param>
    private void HandleMouseDown(MouseButtonType buttonType)
    {
        var hoveredElement = HoveredElement;
        PressedElements[buttonType] = hoveredElement;
        if (hoveredElement == null) return;

        if (_hoveredStack != null && _hoveredSilkyUI != null)
        {
            _hoveredStack.BringToFront(_hoveredSilkyUI);
        }

        switch (buttonType)
        {
            case MouseButtonType.Left:
                hoveredElement.OnLeftMouseDown(new(hoveredElement, MousePosition));
                break;
            case MouseButtonType.Middle:
                hoveredElement.OnMiddleMouseDown(new(hoveredElement, MousePosition));
                break;
            case MouseButtonType.Right:
                hoveredElement.OnRightMouseDown(new(hoveredElement, MousePosition));
                break;
            default: return;
        }
    }

    /// <summary>
    /// 处理按键抬起：
    /// 向按下目标分发 MouseUp，且仅当抬起时仍悬停在同一目标上才分发 Click。
    /// </summary>
    /// <param name="buttonType">触发的鼠标按键。</param>
    private void HandleMouseUp(MouseButtonType buttonType)
    {
        var mouseElement = PressedElements[buttonType];
        if (mouseElement == null) return;

        PressedElements[buttonType] = null;

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

        if (mouseElement == HoveredElement)
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
        }
    }

    /// <summary>
    /// 驱动输入法状态更新。仅焦点元素声明占用输入时生效。
    /// </summary>
    internal void HandleIME()
    {
        if (FocusedElement is not { OccupyPlayerInput: true }) return;

        PlayerInput.WritingText = true;
        Main.instance.HandleIME();
    }

    /// <summary>
    /// 处理键盘/输入法输入并转发到焦点元素。
    /// </summary>
    /// <param name="spriteBatch">当前绘制批次，用于输入面板绘制前后的 ReBegin。</param>
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
