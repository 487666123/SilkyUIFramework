namespace SilkyUIFramework;

[Service]
public class SilkyUIRenderSystem(IServiceProvider provider, SilkyUIRegistrar silkyUIRegistrar)
{
    public static SilkyUIRenderSystem Instance => SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUIRenderSystem>();

    private readonly IServiceProvider _provider = provider;
    private readonly SilkyUIRegistrar _registrar = silkyUIRegistrar;

    private SilkyUISceneStack _globalStack;
    private Dictionary<string, SilkyUISceneStack> _gameStacksByLayer = [];
    private readonly List<string> _layerOrder = [];

    public void Initialize()
    {
        _globalStack = _provider.GetRequiredService<SilkyUISceneStack>();

        foreach (var type in _registrar.GlobalUIBodyTypes)
        {
            var silkyUI = _provider.GetRequiredService<SilkyUI>();
            silkyUI.ScenePriority = type.GetCustomAttribute<RegisterGlobalUIAttribute>()!.Priority;
            silkyUI.SetRoot(_provider.GetRequiredService(type) as BaseBody);
            _globalStack.Push(silkyUI);
        }

        foreach (var (layerNode, _) in _registrar.GameUIBodyTypesByLayer)
        {
            _gameStacksByLayer[layerNode] = _provider.GetRequiredService<SilkyUISceneStack>();
        }

    }

    public void ReloadSilkyUIStacks()
    {
        foreach (var (layerNode, stack) in _gameStacksByLayer)
        {
            if (!_registrar.GameUIBodyTypesByLayer.TryGetValue(layerNode, out var types)) continue;

            stack.Clear();
            foreach (var type in types)
            {
                var ui = SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUI>();

                ui.ScenePriority = type.GetCustomAttribute<RegisterUIAttribute>()!.Priority;
                ui.SetRoot(SilkyUISystem.ServiceProvider.GetRequiredService(type) as BaseBody);

                stack.Push(ui);
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        _globalStack?.Update(gameTime);

        if (Main.gameMenu) return;

        foreach (var stack in OrderedStacks())
        {
            stack.Update(gameTime);
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_globalStack == null) return;

        var orderedUIs = _globalStack.OrderedUIs;
        for (int i = orderedUIs.Count - 1; i >= 0; i--)
        {
            var ui = orderedUIs[i];
            if (!ui.RootNode.GetType().IsDefined(typeof(RegisterGlobalUIAttribute))) continue;

            ui.TransformMatrix = Main.UIScaleMatrix;

            spriteBatch.ReBegin(SpriteSortMode.Deferred,
                null, null, null, SilkyUI.ScissorRasterizerState, null, ui.TransformMatrix);

            ui.Draw(gameTime, Main.spriteBatch);

            spriteBatch.ReBegin(SpriteSortMode.Deferred,
                null, null, null, SilkyUI.ScissorRasterizerState, null, ui.TransformMatrix);
        }
    }

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        _layerOrder.Clear();
        _layerOrder.AddRange(layers.Select(l => l.Name));

        foreach (var (layerNode, stack) in _gameStacksByLayer)
        {
            int index = layers.FindIndex(l => l.Name.Equals(layerNode));
            if (index < 0) continue;

            foreach (var silkyUI in stack.OrderedUIs)
            {
                if (silkyUI.RootNode.GetType().GetCustomAttribute<RegisterUIAttribute>() is not { } registerUI) continue;

                layers.Insert(index + 1, new SilkyUILayer(silkyUI, registerUI.Name, registerUI.InterfaceScaleType));
            }
        }
    }

    public UIView HitTest(Vector2 position)
    {
        if (_globalStack != null)
        {
            var target = _globalStack.HitTest(position);
            if (target != null) return target;
        }

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

    private IEnumerable<SilkyUISceneStack> OrderedStacks()
        => _layerOrder.Select(layer => _gameStacksByLayer.TryGetValue(layer, out var v) ? v : null)
                       .Where(v => v != null)
                       .Reverse();

    private static bool ContainsUI(SilkyUISceneStack stack, SilkyUI silkyUI)
        => stack.OrderedUIs.Contains(silkyUI);
}
