namespace SilkyUIFramework;

public static class GraphicsDeviceHelper
{
    public static int BackBufferWidth => Main.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
    public static int BackBufferHeight => Main.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

    public static Size GetBackBufferSize()
    {
        var pp = Main.graphics.GraphicsDevice.PresentationParameters;
        return new Size(pp.BackBufferWidth, pp.BackBufferHeight);
    }

    public static Size GetBackBufferSizeByUIScale() => GetBackBufferSize() / Main.UIScale;

    public static GraphicsDeviceStates BackupStates(this SpriteBatch spriteBatch)
    {
        return GraphicsDeviceStates.BackupStates(spriteBatch.GraphicsDevice, spriteBatch);
    }
}
