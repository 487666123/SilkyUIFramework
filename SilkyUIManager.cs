namespace SilkyUIFramework;

[Service]
public class SilkyUIManager(
    UISceneManager sceneManager,
    SilkyUIRenderSystem renderSystem,
    SilkyUILayerAdapter layerAdapter,
    SilkyUIInputState inputState)
{
    private readonly UISceneManager _sceneManager = sceneManager;
    private readonly SilkyUIRenderSystem _renderSystem = renderSystem;
    private readonly SilkyUILayerAdapter _layerAdapter = layerAdapter;
    private readonly SilkyUIInputState _inputState = inputState;

    public static SilkyUIManager Instance => SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUIManager>();

    public void Initialize() => _sceneManager.Initialize();

    public void ReloadGameScenes() => _sceneManager.ReloadGameScenes();

    public void Update(GameTime gameTime)
    {
        if (Main.hideUI) return;

        _inputState.Update();
        _sceneManager.Update(gameTime);
    }

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        _layerAdapter.ModifyInterfaceLayers(layers);
    }

    public void HandleIME() => _inputState.HandleIME();

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        _renderSystem.DrawGlobal(gameTime, spriteBatch);
        _inputState.HandleInput(Main.spriteBatch);
    }
}
