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
    public static SilkyUIInputState Instance => SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUIInputState>();

    /// <summary>渲染系统引用，用于查询鼠标命中的 UI 元素。</summary>
    readonly SilkyUIRenderSystem _renderSystem = renderSystem;

    /// <summary>支持处理的鼠标按键集合。</summary>
    public static MouseButtonType[] MouseButtons { get; } = [.. Enum.GetValues(typeof(MouseButtonType)).Cast<MouseButtonType>()];

    /// <summary>当前帧鼠标屏幕坐标。</summary>
    Vector2 _mousePosition;

    /// <summary>当前帧鼠标按键状态。</summary>
    public MouseStatus CurrentMouse { get; } = new();

    /// <summary>上一帧鼠标按键状态。</summary>
    public MouseStatus PreviousMouse { get; } = new();

    /// <summary>当前获得焦点的目标。</summary>
    public UIView FocusTarget { get; private set; }

    /// <summary>当前鼠标悬停目标。</summary>
    public UIView HoverTarget { get; private set; }

    /// <summary>上一帧鼠标悬停目标。</summary>
    public UIView PreviousHoverTarget { get; private set; }

    /// <summary>
    /// 每个鼠标按键在按下瞬间对应的元素。
    /// 用于抬起时判断事件目标与 Click 判定。
    /// </summary>
    readonly Dictionary<MouseButtonType, UIView> _pressTargetsByButton = [];

    internal void Update()
    {
        UpdateMouseStatus();
        UpdateHoverTarget();
        UpdateButtonEvent();
        UpdateScrollEvent();
    }

    /// <summary>驱动输入法状态更新。仅焦点目标声明占用输入时生效。</summary>
    internal void HandleIME()
    {
        if (FocusTarget is not { OccupyPlayerInput: true }) return;

        PlayerInput.WritingText = true;
        Main.instance.HandleIME();
    }

    /// <summary>处理键盘/输入法输入并转发到焦点目标。</summary>
    /// <param name="spriteBatch">当前绘制批次，用于输入面板绘制前后的 ReBegin。</param>
    internal void HandleInput(SpriteBatch spriteBatch)
    {
        if (FocusTarget is not { OccupyPlayerInput: true }) return;

        if (!Main.hasFocus) return; // 焦点不在游戏

        Main.oldInputText = Main.inputText;
        Main.inputText = Keyboard.GetState();

        spriteBatch.ReBegin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

        var imeService = Platform.Get<IImeService>();
        Main.instance.DrawWindowsIMEPanel(FocusTarget.InputMethodPosition);

        FocusTarget.HandlePlayerInput(imeService.CandidateCount > 0);

        spriteBatch.ReBegin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
    }

    /// <summary>刷新鼠标状态缓存：将当前状态写入上一帧，再采样本帧状态。</summary>
    void UpdateMouseStatus()
    {
        PreviousMouse.SetState(CurrentMouse);
        CurrentMouse.SetState(Main.mouseLeft, Main.mouseMiddle, Main.mouseRight);

        _mousePosition = new Vector2(Main.mouseX, Main.mouseY);
    }

    /// <summary>更新当前悬停目标，并在目标变化时触发 MouseLeave/MouseEnter。</summary>
    void UpdateHoverTarget()
    {
        var element = _renderSystem.HitTest(_mousePosition);

        PreviousHoverTarget = HoverTarget;

        if (HoverTarget == element) return;
        HoverTarget = element;

        PreviousHoverTarget?.OnMouseLeave(new(PreviousHoverTarget, _mousePosition));
        HoverTarget?.OnMouseEnter(new(HoverTarget, _mousePosition));
    }

    /// <summary>根据按键边沿变化分发 MouseDown/MouseUp/Click，并更新焦点。</summary>
    void UpdateButtonEvent()
    {
        UpdateFocusElement();

        foreach (var buttonType in MouseButtons)
        {
            if (CurrentMouse[buttonType])
            {
                if (PreviousMouse[buttonType]) continue;
                HandleMouseDown(buttonType);
                UpdateFocusElement(HoverTarget);
            }
            else
            {
                if (!PreviousMouse[buttonType]) continue;
                HandleMouseUp(buttonType);
            }
        }
    }

    /// <summary>分发滚轮事件，并在需要时将焦点目标注册为输入接管者。</summary>
    void UpdateScrollEvent()
    {
        var target = HoverTarget;

        if (PlayerInput.ScrollWheelDeltaForUI != 0)
        {
            target?.OnMouseWheel(new(target, _mousePosition, PlayerInput.ScrollWheelDeltaForUI));
        }

        if (FocusTarget is { OccupyPlayerInput: true } inputElement)
            Main.CurrentInputTextTakerOverride = inputElement;
    }

    /// <summary>校验当前焦点是否仍然有效；失效时清空并触发 LostFocus。</summary>
    internal void UpdateFocusElement()
    {
        if (FocusTarget?.SilkyUI?.RootNode is { Enabled: true, IsInteractable: true }) return;

        var previousFocusTarget = FocusTarget;
        FocusTarget = null;

        previousFocusTarget?.OnLostFocus(new(previousFocusTarget, _mousePosition));
    }

    /// <summary>将焦点切换到指定悬停目标，并触发 LostFocus/GotFocus。</summary>
    /// <param name="hoverTarget">候选焦点元素。</param>
    internal void UpdateFocusElement(UIView hoverTarget)
    {
        if (hoverTarget == FocusTarget) return;

        var previousFocusTarget = FocusTarget;
        FocusTarget = hoverTarget;

        previousFocusTarget?.OnLostFocus(new(previousFocusTarget, _mousePosition));
        FocusTarget?.OnGotFocus(new(FocusTarget, _mousePosition));
    }

    /// <summary>
    /// 处理按键按下：
    /// 记录按下目标，必要时置顶 UI，并向目标分发 MouseDown 事件。
    /// </summary>
    /// <param name="buttonType">触发的鼠标按键。</param>
    void HandleMouseDown(MouseButtonType buttonType)
    {
        var hoverTarget = HoverTarget;
        _pressTargetsByButton[buttonType] = hoverTarget;
        if (hoverTarget == null) return;

        _renderSystem.Activate(hoverTarget);

        switch (buttonType)
        {
            case MouseButtonType.Left:
                hoverTarget.OnLeftMouseDown(new(hoverTarget, _mousePosition));
                break;
            case MouseButtonType.Middle:
                hoverTarget.OnMiddleMouseDown(new(hoverTarget, _mousePosition));
                break;
            case MouseButtonType.Right:
                hoverTarget.OnRightMouseDown(new(hoverTarget, _mousePosition));
                break;
            default: return;
        }
    }

    /// <summary>
    /// 处理按键抬起：
    /// 向按下目标分发 MouseUp，且仅当抬起时仍悬停在同一目标上才分发 Click。
    /// </summary>
    /// <param name="buttonType">触发的鼠标按键。</param>
    void HandleMouseUp(MouseButtonType buttonType)
    {
        var target = _pressTargetsByButton[buttonType];
        if (target == null) return;

        _pressTargetsByButton[buttonType] = null;

        switch (buttonType)
        {
            case MouseButtonType.Left:
                target.OnLeftMouseUp(new UIMouseEvent(target, _mousePosition));
                break;
            case MouseButtonType.Middle:
                target.OnMiddleMouseUp(new UIMouseEvent(target, _mousePosition));
                break;
            case MouseButtonType.Right:
                target.OnRightMouseUp(new UIMouseEvent(target, _mousePosition));
                break;
            default: return;
        }

        if (target != HoverTarget) return;

        switch (buttonType)
        {
            case MouseButtonType.Left:
                target.OnLeftMouseClick(new UIMouseEvent(target, _mousePosition));
                break;
            case MouseButtonType.Middle:
                target.OnMiddleMouseClick(new UIMouseEvent(target, _mousePosition));
                break;
            case MouseButtonType.Right:
                target.OnRightMouseClick(new UIMouseEvent(target, _mousePosition));
                break;
            default: return;
        }
    }
}
