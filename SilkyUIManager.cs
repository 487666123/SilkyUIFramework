namespace SilkyUIFramework;

[Service]
public class SilkyUIManager(SilkyUIRenderSystem renderSystem, SilkyUIInputState inputState)
{
    private readonly SilkyUIRenderSystem _renderSystem = renderSystem;
    private readonly SilkyUIInputState _inputState = inputState;

    public void Initialize() => _renderSystem.Initialize();

    public void Update(GameTime gameTime)
    {
        if (Main.hideUI) return;

        _inputState.Update();
        _renderSystem.Update(gameTime);
    }

    public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        _renderSystem.ModifyInterfaceLayers(layers);
    }

    public void HandleIME() => _inputState.HandleIME();

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        _renderSystem.Draw(gameTime, spriteBatch);
        _inputState.HandleInput(Main.spriteBatch);
    }

    public bool TryGetInstance<TBody>(out TBody body) where TBody : BaseBody => _renderSystem.TryGetInstance(out body);
}
