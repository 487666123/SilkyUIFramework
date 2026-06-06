namespace SilkyUIFramework;

[Service]
public class SilkyUILayerAdapter(UISceneManager sceneManager, SilkyUIRenderSystem renderSystem)
{
    private readonly UISceneManager _sceneManager = sceneManager;
    private readonly SilkyUIRenderSystem _renderSystem = renderSystem;

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        _sceneManager.SetLayerOrder(layers.Select(l => l.Name));

        foreach (var (layerNode, stack) in _sceneManager.GameStacksByLayer)
        {
            int index = layers.FindIndex(l => l.Name.Equals(layerNode));
            if (index < 0) continue;

            foreach (var silkyUI in stack.OrderedUIs)
            {
                if (silkyUI.RootNode.GetType().GetCustomAttribute<RegisterUIAttribute>() is not { } registerUI) continue;

                layers.Insert(index + 1, new SilkyUILayer(
                    silkyUI,
                    _renderSystem,
                    registerUI.Name,
                    registerUI.InterfaceScaleType));
            }
        }
    }
}
