namespace SilkyUIFramework;

public static class GraphicsDeviceHelper
{
    public static Size GetBackBufferSize()
    {
        var pp = Main.graphics.GraphicsDevice.PresentationParameters;
        return new Size(pp.BackBufferWidth, pp.BackBufferHeight);
    }

    public static Size GetBackBufferSizeByUIScale() => GetBackBufferSize() / Main.UIScale;
}
