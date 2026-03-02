using SilkyUIFramework.Layout;

namespace SilkyUIFramework.Elements;

public abstract partial class BaseBody : UIElementGroup
{
    public virtual bool Enabled { get; set; } = true;
    public virtual bool IsInteractable => true;

    protected virtual bool AvailableItem { get; set; } = false;
    protected virtual bool AvailableScroll { get; set; } = false;

    protected BaseBody()
    {
        ScreenshotSavePath = GetDefaultScreenshotPath(GetType().Name);

        SetSize(16f * 30f, 9f * 30f);
        Gap = new Size(10f);

        Positioning = Positioning.Fixed;
        Border = 2f;
        BorderColor = Color.Black;
        BackgroundColor = Color.White * 0.25f;

        LayoutType = LayoutType.Flexbox;
        FlexDirection = FlexDirection.Column;
        MainAlignment = MainAlignment.Start;
        FlexWrap = false;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        WatchScreenSize();

        if (IsMouseHovering)
        {
            if (!AvailableScroll)
            {
                PlayerInput.LockVanillaMouseScroll("SilkyUIFramework");
            }

            if (!AvailableItem)
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        base.UpdateStatus(gameTime);
    }

    protected Size oldScreenSize = GraphicsDeviceHelper.GetBackBufferSize();

    protected void WatchScreenSize()
    {
        var newScreenSize = GraphicsDeviceHelper.GetBackBufferSizeByUIScale();
        if (oldScreenSize == newScreenSize) return;

        OnScreenSizeChanged(newScreenSize, oldScreenSize);
        oldScreenSize = newScreenSize;
    }

    protected virtual void OnScreenSizeChanged(Size newScreenSize, Size oldScreenSize)
    {
        MarkLayoutDirty();
    }

    public override UIView GetElementAt(Vector2 mousePosition)
    {
        if (DisableMouseInteraction) return null;
        if (!ContainsPoint(mousePosition)) return null;

        foreach (var child in ElementsInOrder.Reverse<UIView>())
        {
            var target = child.GetElementAt(mousePosition);
            if (target != null) return target;
        }

        // 所有子元素都不符合条件, 如果自身不忽略鼠标交互, 则返回自己
        return IgnoreMouseInteraction ? null : this;
    }

    /// <summary>
    /// BaseBody 作为顶级 UI 容器，始终需要独立执行完整布局管线。
    /// 即使定位类型不是脱离文档流，也不能依赖父级布局阶段驱动。
    /// </summary>
    protected override void HandleDirtyLayoutUpdate() => RunIndependentLayoutPass();
}
