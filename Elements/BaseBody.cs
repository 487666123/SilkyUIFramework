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
        FinallyDrawBorder = true;
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
    /// 更新布局，不调用基类实现以确保使用标准的盒子布局算法。
    /// 基类 UIElementGroup.UpdateLayout() 会根据 Positioning.IsFree 选择不同的布局算法（UpdateBoxLayout 或 UpdateFlowLayout），
    /// 但 BaseBody 作为顶级 UI 容器，始终需要使用完整的盒子布局计算以确保正确的尺寸和位置。
    /// </summary>
    public override void UpdateLayout()
    {
        if (LayoutIsDirty)
        {
            UpdateBoxLayout();
            CleanupDirtyMark();
        }

        foreach (var child in ElementsCache)
        {
            child.UpdateLayout();
        }
    }
}