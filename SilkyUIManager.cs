namespace SilkyUIFramework;

[Service]
public class SilkyUIManager(IServiceProvider provider, SilkyUIRegistrar registrar, SilkyUIRenderSystem renderSystem, SilkyUIInputState inputState)
{
    private readonly IServiceProvider _provider = provider;
    private readonly SilkyUIRegistrar _registrar = registrar;
    private readonly SilkyUIRenderSystem _renderSystem = renderSystem;
    private readonly SilkyUIInputState _inputState = inputState;

    public void Initialize()
    {
        if (Main.netMode == NetmodeID.Server) return;

        var globalGroup = _provider.GetRequiredService<SilkyUIGroup>();

        foreach (var type in _registrar.BodyTypesForGlobalUI)
        {
            var silkyUI = _provider.GetRequiredService<SilkyUI>();
            silkyUI.Priority = type.GetCustomAttribute<RegisterGlobalUIAttribute>()!.Priority;
            silkyUI.SetBody(_provider.GetRequiredService(type) as BaseBody);
            globalGroup.Add(silkyUI);
        }

        var gameGroups = new Dictionary<string, SilkyUIGroup>();
        foreach (var (layerNode, _) in _registrar.BodyTypesForGameUI)
        {
            gameGroups[layerNode] = _provider.GetRequiredService<SilkyUIGroup>();
        }

        _renderSystem.SetGroups(globalGroup, gameGroups);
    }

    public void Update(GameTime gameTime)
    {
        if (Main.hideUI) return;

        UpdateInput();
        _renderSystem.Update(gameTime);
    }

    private void UpdateInput()
    {
        _inputState.UpdateMouseStatus();
        _inputState.UpdateHoverTarget();
        _inputState.UpdateMouseEvent();
        _inputState.UpdateScrollEvent();
    }

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        _renderSystem.ModifyInterfaceLayers(layers);
    }

    public void HandleIME() => _inputState.HandleIME();

    public void Draw(GameTime gameTime)
    {
        _renderSystem.Draw(gameTime);
        _inputState.HandleInput(Main.spriteBatch);
    }

    public bool TryGetInstance<TBody>(out TBody body) where TBody : BaseBody => _renderSystem.TryGetInstance(out body);
}
