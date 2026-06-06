namespace SilkyUIFramework;

public class SilkyUILayer(
    SilkyUI silkyUI,
    SilkyUIRenderSystem renderSystem,
    string name,
    InterfaceScaleType scaleType) : GameInterfaceLayer(name, scaleType)
{
    private readonly SilkyUI _silkyUI = silkyUI;
    private readonly SilkyUIRenderSystem _renderSystem = renderSystem;

    public SilkyUILayer(SilkyUI silkyUI, string name, InterfaceScaleType scaleType)
        : this(silkyUI, SilkyUIRenderSystem.Instance, name, scaleType) { }

    public override bool DrawSelf()
    {
        var matrix = ScaleType switch
        {
            InterfaceScaleType.Game => Main.GameViewMatrix.ZoomMatrix,
            InterfaceScaleType.UI => Main.UIScaleMatrix,
            { } => Matrix.Identity,
        };

        SilkyUIRenderSystem.DrawScene(_silkyUI, Main.gameTimeCache, Main.spriteBatch, matrix);
        return true;
    }
}
