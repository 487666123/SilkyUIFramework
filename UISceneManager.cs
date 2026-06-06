namespace SilkyUIFramework;

/// <summary>
/// 管理 SilkyUI 运行时场景。
/// 负责 UI 实例创建、场景栈维护、更新顺序、命中测试、激活置顶和 Body 实例查询。
/// </summary>
[Service]
public class UISceneManager(IServiceProvider provider, BodyTypeRegistry bodyTypeRegistry) : IUIInteractionService
{
    public static UISceneManager Instance => SilkyUISystem.ServiceProvider.GetRequiredService<UISceneManager>();

    private readonly IServiceProvider _provider = provider;

    private readonly BodyTypeRegistry _bodyTypeRegistry = bodyTypeRegistry;

    private SilkyUISceneStack _globalStack;

    private readonly Dictionary<string, SilkyUISceneStack> _gameStacksByLayer = [];

    private readonly List<string> _layerOrder = [];

    private bool _initialized;

    public IReadOnlyList<SilkyUI> GlobalUIs => _globalStack?.OrderedUIs ?? Array.Empty<SilkyUI>();

    public IReadOnlyDictionary<string, SilkyUISceneStack> GameStacksByLayer => _gameStacksByLayer;

    /// <summary>
    /// 全局 UI 在初始化阶段创建；游戏内 UI 只创建空栈，具体实例延迟到玩家进世界时重载。
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;

        _globalStack = _provider.GetRequiredService<SilkyUISceneStack>();

        // 全局 UI 生命周期跨世界存在，因此初始化阶段直接创建并压入全局栈。
        foreach (var registration in _bodyTypeRegistry.GlobalRegistrations)
        {
            var silkyUI = CreateSilkyUI(registration);
            _globalStack.Push(silkyUI);
        }

        // 游戏内 UI 依赖世界状态，这里只为每个 layer 节点准备栈，实例延迟到进世界时创建。
        foreach (var (layerNode, _) in _bodyTypeRegistry.GameRegistrationsByLayerNode)
        {
            _gameStacksByLayer[layerNode] = _provider.GetRequiredService<SilkyUISceneStack>();
        }

        _initialized = true;
    }

    /// <summary>
    /// 进世界时重建游戏内 UI，避免跨世界保留旧状态。
    /// </summary>
    public void ReloadGameScenes()
    {
        foreach (var (layerNode, stack) in _gameStacksByLayer)
        {
            if (!_bodyTypeRegistry.GameRegistrationsByLayerNode.TryGetValue(layerNode, out var registrations)) continue;

            // 清理旧世界中的 UI 根节点，再为当前世界重新创建 Body 实例。
            stack.Clear();
            foreach (var registration in registrations)
            {
                stack.Push(CreateSilkyUI(registration));
            }
        }
    }

    /// <summary>
    /// 主菜单中只更新全局 UI，保持游戏内 UI 只在世界中运行。
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _globalStack?.Update(gameTime);

        if (Main.gameMenu) return;

        foreach (var stack in OrderedStacks())
        {
            stack.Update(gameTime);
        }
    }

    /// <summary>
    /// 缓存 Terraria 当前 interface layer 顺序。
    /// 后续游戏内 UI 更新和命中测试会按该顺序映射到对应的 UI 栈。
    /// </summary>
    public void SetLayerOrder(IEnumerable<string> layerNames)
    {
        _layerOrder.Clear();
        _layerOrder.AddRange(layerNames);
    }

    /// <summary>
    /// 反向遍历保持原行为：越靠后的 layer 越先参与更新和命中测试。
    /// </summary>
    public IEnumerable<SilkyUISceneStack> OrderedStacks()
        => _layerOrder.Select(layer => _gameStacksByLayer.TryGetValue(layer, out var v) ? v : null)
                      .Where(v => v != null)
                      // 后绘制的 layer 视觉上更靠前，输入命中也应优先检查它们。
                      .Reverse();

    /// <summary>
    /// 全局 UI 优先于游戏内 UI；游戏菜单中跳过游戏内 UI。
    /// </summary>
    public UIView HitTest(Vector2 position)
    {
        // 全局 UI 作为顶层覆盖物优先处理输入，例如菜单、弹窗或遮罩。
        if (_globalStack != null)
        {
            var target = _globalStack.HitTest(position);
            if (target != null) return target;
        }

        // 主菜单中不检查游戏内 UI，保持它们只在世界内参与交互。
        if (!Main.gameMenu)
        {
            foreach (var stack in OrderedStacks())
            {
                var target = stack.HitTest(position);
                if (target != null) return target;
            }
        }

        return null;
    }

    /// <summary>
    /// 置顶只在元素所属栈内生效，不跨越全局 UI 与游戏内 UI 的边界。
    /// </summary>
    public void Activate(UIView element)
    {
        var silkyUI = element?.SilkyUI;
        if (silkyUI == null) return;

        if (_globalStack != null && ContainsUI(_globalStack, silkyUI))
        {
            _globalStack.BringToFront(silkyUI);
            return;
        }

        foreach (var stack in _gameStacksByLayer.Values)
        {
            if (!ContainsUI(stack, silkyUI)) continue;

            stack.BringToFront(silkyUI);
            return;
        }
    }

    public bool TryGetInstance<TBody>(out TBody body) where TBody : BaseBody
    {
        foreach (var ui in _globalStack?.OrderedUIs ?? [])
        {
            if (ui.RootNode is TBody tBody) { body = tBody; return true; }
        }

        foreach (var stack in _gameStacksByLayer.Values)
        {
            foreach (var ui in stack.OrderedUIs)
            {
                if (ui.RootNode is TBody tBody) { body = tBody; return true; }
            }
        }

        body = null;
        return false;
    }

    public List<TBody> GetInstances<TBody>() where TBody : BaseBody
    {
        var bodys = new List<TBody>();

        foreach (var ui in _globalStack?.OrderedUIs ?? [])
        {
            if (ui.RootNode is TBody tBody)
            {
                bodys.Add(tBody);
            }
        }

        foreach (var (_, stack) in _gameStacksByLayer)
        {
            foreach (var ui in stack.OrderedUIs)
            {
                if (ui.RootNode is TBody tBody)
                {
                    bodys.Add(tBody);
                }
            }
        }

        return bodys;
    }

    static SilkyUI CreateSilkyUI(SilkyUIRegistration registration)
    {
        var silkyUI = new SilkyUI
        {
            ScenePriority = registration.Priority
        };
        silkyUI.SetRoot(Activator.CreateInstance(registration.BodyType) as BaseBody);
        return silkyUI;
    }

    static bool ContainsUI(SilkyUISceneStack stack, SilkyUI ui) => stack.OrderedUIs.Contains(ui);
}
