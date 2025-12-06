namespace SilkyUIFramework;

[Service]
public class SilkyUIRenderSystem(SilkyUIRegistrar silkyUIRegistrar)
{
    private readonly SilkyUIRegistrar _silkyUIRegistrar = silkyUIRegistrar;

    private SilkyUIGroup _globalGroup;
    private IReadOnlyDictionary<string, SilkyUIGroup> _gameGroups;

    private readonly List<string> _layerOrders = [];

    public void SetGroups(SilkyUIGroup global, Dictionary<string, SilkyUIGroup> games)
    {
        _globalGroup = global;
        _gameGroups = games.AsReadOnly();
    }

    public void ReloadGameGroups()
    {
        foreach (var (layerNode, group) in _gameGroups)
        {
            group.Clear();

            if (!_silkyUIRegistrar.BodyTypesForGameUI.TryGetValue(layerNode, out var types)) continue;

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
        _globalGroup?.UpdateUI(gameTime);

        if (Main.gameMenu) return;

        foreach (var group in OrderedGroups())
        {
            group.UpdateUI(gameTime);
        }
    }

    public void Draw(GameTime gameTime) => _globalGroup?.Draw(gameTime);

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        _layerOrders.Clear();
        _layerOrders.AddRange(layers.Select(l => l.Name));

        foreach (var (layerNode, group) in _gameGroups)
        {
            int index = layers.FindIndex(l => l.Name.Equals(layerNode));
            if (index >= 0) group.ModifyInterfaceLayers(layers, index);
        }
    }

    public void GetHoverTarget(out SilkyUIGroup silkyUIGroup, out SilkyUI silkyUI, out UIView element)
    {
        if (_globalGroup != null)
        {
            _globalGroup.GetHoverTarget(out silkyUI, out element);

            if (element != null)
            {
                silkyUIGroup = _globalGroup;
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
        foreach (var ui in _globalGroup?.SilkyUIs ?? [])
        {
            if (ui.RootNode is TBody tBody) { body = tBody; return true; }
        }

        foreach (var group in _gameGroups.Values)
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
        => _layerOrders.Select(layer => _gameGroups.TryGetValue(layer, out var v) ? v : null)
                       .Where(v => v != null)
                       .Reverse();
}
