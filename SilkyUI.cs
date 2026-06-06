namespace SilkyUIFramework;

/// <summary>
/// SilkyUI 根容器，负责维护 UI 根节点、矩阵变换，并驱动更新与绘制流程。
/// </summary>
[Service(ServiceLifetime.Transient)]
public class SilkyUI
{
    /// <summary>
    /// UI 实例优先级。通常由管理器按该值排序决定更新/绘制顺序。
    /// </summary>
    public int ScenePriority { get; set; }

    /// <summary>
    /// 当前 UI 树根节点。可能为 <see langword="null"/>。
    /// </summary>
    public BaseBody RootNode { get; private set; }

    private Matrix _matrix;

    /// <summary>
    /// UI 变换矩阵引用。以引用返回便于调用方原地修改矩阵。
    /// </summary>
    public ref Matrix TransformMatrix => ref _matrix;

    /// <summary>
    /// 设置并切换根节点。
    /// 会触发旧节点退出树（ExitTree）与新节点进入树（EnterTree）的生命周期回调。
    /// </summary>
    public void SetRoot(BaseBody baseBody)
    {
        if (RootNode == baseBody) return;

        if (baseBody is { SilkyUI: not null })
            throw new InvalidOperationException($"Cannot attach body '{baseBody.GetType().FullName}' because it is already attached to another SilkyUI instance.");

        RootNode?.HandleExitTree();

        RootNode = baseBody;
        if (RootNode is null) return;
        RootNode.Initialize();
        RootNode.HandleEnterTree(this);
    }

    /// <summary>
    /// 对当前 UI 树执行命中测试，返回最上层可交互元素。
    /// </summary>
    public UIView HitTest(Vector2 position)
    {
        if (RootNode is not { Enabled: true, IsInteractable: true }) return null;

        PlayerInputHelper.SetZoom(TransformMatrix);

        return RootNode.GetElementAt(position);
    }

    /// <summary>
    /// 驱动 UI 逻辑更新，不执行绘制。
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (RootNode == null) return;
        if (!RootNode.Enabled) return;

        RootNode.HandleUpdate(gameTime);
    }

    /// <summary>
    /// 执行 UI 绘制主流程：初始化、布局更新、状态更新与最终绘制。
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (RootNode == null) return;

        PlayerInputHelper.SetZoom(TransformMatrix);

        RootNode.Initialize();

        if (!RootNode.Enabled) return;

        RootNode.UpdateLayout();
        RootNode.UpdatePosition();
        RootNode.UpdateElementsOrder();

        // 更新 UI 的各类运行状态（例如动画），状态变化可能影响后续布局与显示。
        RootNode.HandleUpdateStatus(gameTime);

        if (!RootNode.Enabled) return;

        RootNode.UpdateLayout();
        RootNode.UpdatePosition();
        RootNode.UpdateElementsOrder();

        RootNode.HandleDraw(gameTime, spriteBatch);
    }

    /// <summary>
    /// OverflowHidden 裁剪所用的光栅化状态。
    /// 开启 ScissorTest 并关闭剔除，避免 UI 平面元素被背面剔除。
    /// </summary>
    public static RasterizerState ScissorRasterizerState { get; } = new RasterizerState
    {
        CullMode = CullMode.None,
        ScissorTestEnable = true,
    };
}
