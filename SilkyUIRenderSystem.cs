namespace SilkyUIFramework;

[Service]
public class SilkyUIRenderSystem(UISceneManager sceneManager)
{
    public static SilkyUIRenderSystem Instance => SilkyUISystem.ServiceProvider.GetRequiredService<SilkyUIRenderSystem>();

    private readonly UISceneManager _sceneManager = sceneManager;

    public void DrawGlobal(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var orderedUIs = _sceneManager.GlobalUIs;
        for (int i = orderedUIs.Count - 1; i >= 0; i--)
        {
            var ui = orderedUIs[i];
            if (!ui.RootNode.GetType().IsDefined(typeof(RegisterGlobalUIAttribute))) continue;

            DrawScene(ui, gameTime, spriteBatch, Main.UIScaleMatrix);
        }
    }

    public static void DrawScene(SilkyUI ui, GameTime gameTime, SpriteBatch spriteBatch, Matrix matrix)
    {
        ui.TransformMatrix = matrix;

        spriteBatch.ReBegin(SpriteSortMode.Deferred,
            null, null, null, SilkyUI.ScissorRasterizerState, null, ui.TransformMatrix);

        ui.Draw(gameTime, spriteBatch);
    }
}
