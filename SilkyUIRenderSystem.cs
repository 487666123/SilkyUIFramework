namespace SilkyUIFramework;

[Service]
public class SilkyUIRenderSystem(IServiceProvider provider, SilkyUIRegistrar silkyUIRegistrar)
{
    public static SilkyUIRenderSystem Instance => SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUIRenderSystem>();

    private readonly IServiceProvider _provider = provider;
    private readonly SilkyUIRegistrar _registrar = silkyUIRegistrar;

    private SilkyUIGroup _global;
    private Dictionary<string, SilkyUIGroup> _game;
    private readonly List<string> _layerOrder = [];

    public void Initialize()
    {
        _global = _provider.GetRequiredService<SilkyUIGroup>();

        foreach (var type in _registrar.GlobalUIBodyTypes)
        {
            var silkyUI = _provider.GetRequiredService<SilkyUI>();
            silkyUI.Priority = type.GetCustomAttribute<RegisterGlobalUIAttribute>()!.Priority;
            silkyUI.SetBody(_provider.GetRequiredService(type) as BaseBody);
            _global.Add(silkyUI);
        }

        _game = [];

        foreach (var (layerNode, _) in _registrar.GameUIBodyTypesByLayer)
        {
            _game[layerNode] = _provider.GetRequiredService<SilkyUIGroup>();
        }

    }

    public void ReloadSilkyUIGroups()
    {
        foreach (var (layerNode, group) in _game)
        {
            if (!_registrar.GameUIBodyTypesByLayer.TryGetValue(layerNode, out var types)) continue;

            group.Clear();
            foreach (var type in types)
            {
                var ui = SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUI>();

                ui.Priority = type.GetCustomAttribute<RegisterUIAttribute>()!.Priority;
                ui.SetBody(SilkyUISystem.ServiceProvider.GetRequiredService(type) as BaseBody);

                group.Add(ui);
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        _global?.UpdateUI(gameTime);

        if (Main.gameMenu) return;

        foreach (var group in OrderedGroups())
        {
            group.UpdateUI(gameTime);
        }
    }

    public void Draw(GameTime gameTime) => _global?.Draw(gameTime);

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        _layerOrder.Clear();
        _layerOrder.AddRange(layers.Select(l => l.Name));

        foreach (var (layerNode, group) in _game)
        {
            int index = layers.FindIndex(l => l.Name.Equals(layerNode));
            if (index >= 0) group.ModifyInterfaceLayers(layers, index);
        }
    }

    public void GetHoverTarget(out SilkyUIGroup silkyUIGroup, out SilkyUI silkyUI, out UIView element)
    {
        if (_global != null)
        {
            _global.GetHoverTarget(out silkyUI, out element);

            if (element != null)
            {
                silkyUIGroup = _global;
                return;
            }
        }

        if (!Main.gameMenu)
        {
            foreach (var group in OrderedGroups())
            {
                group.GetHoverTarget(out silkyUI, out element);

                if (element != null)
                {
                    silkyUIGroup = group;
                    return;
                }
            }
        }

        silkyUIGroup = null;
        silkyUI = null;
        element = null;
    }

    public bool TryGetInstance<TBody>(out TBody body) where TBody : BaseBody
    {
        foreach (var ui in _global?.SilkyUIs ?? [])
        {
            if (ui.RootNode is TBody tBody) { body = tBody; return true; }
        }

        foreach (var group in _game.Values)
        {
            foreach (var ui in group.SilkyUIs)
            {
                if (ui.RootNode is TBody tBody) { body = tBody; return true; }
            }
        }

        body = null;
        return false;
    }

    private IEnumerable<SilkyUIGroup> OrderedGroups()
        => _layerOrder.Select(layer => _game.TryGetValue(layer, out var v) ? v : null)
                       .Where(v => v != null)
                       .Reverse();
}
