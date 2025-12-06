namespace SilkyUIFramework;

[Service(ServiceLifetime.Transient)]
public class SilkyUI
{
    public int Priority { get; set; }
    public BaseBody RootNode { get; private set; }
    public Matrix TransformMatrix { get; set; }

    public void SetBody(BaseBody baseBody)
    {
        if (RootNode == baseBody || baseBody is { SilkyUI: not null }) return;

        if (RootNode != null)
            RuntimeSafeHelper.SafeInvoke(RootNode.HandleExitTree);

        RootNode = baseBody;

        if (RootNode != null)
            RuntimeSafeHelper.SafeInvoke(() => RootNode.HandleEnterTree(this));
    }

    public UIView GetHoverElement()
    {
        if (RootNode is not { Enabled: true, IsInteractable: true }) return null;

        PlayerInputHelper.SetZoom(TransformMatrix);

        return RootNode.GetElementAt(SilkyUIInputState.MousePosition);
    }

    public void Update(GameTime gameTime)
    {
        if (RootNode == null) return;
        if (!RootNode.Enabled) return;

        RootNode.HandleUpdate(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (RootNode == null) return;

        PlayerInputHelper.SetZoom(TransformMatrix);

        RootNode.Initialize();

        if (!RootNode.Enabled) return;

        RootNode.UpdateLayout();
        RootNode.UpdatePosition();
        RootNode.UpdateElementsOrder();

        // 更新 UI 的各种状态，比如动画
        RootNode.HandleUpdateStatus(gameTime);

        if (!RootNode.Enabled) return;

        RootNode.UpdateLayout();
        RootNode.UpdatePosition();
        RootNode.UpdateElementsOrder();

        RootNode.HandleDraw(gameTime, spriteBatch);
    }

    public static RasterizerState RasterizerStateForOverflowHidden { get; } = new RasterizerState
    {
        CullMode = CullMode.None,
        ScissorTestEnable = true,
    };
}
