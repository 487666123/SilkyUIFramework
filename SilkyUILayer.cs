namespace SilkyUIFramework;

public class SilkyUILayer(SilkyUI silkyUI, string name, InterfaceScaleType scaleType)
    : GameInterfaceLayer(name, scaleType)
{
    private SilkyUI SilkyUI { get; } = silkyUI;

    public override bool DrawSelf()
    {
        var sb = Main.spriteBatch;
        var matrix = ScaleType switch
        {
            InterfaceScaleType.Game => Main.GameViewMatrix.ZoomMatrix,
            InterfaceScaleType.UI => Main.UIScaleMatrix,
            { } => Matrix.Identity,
        };

        sb.End();
        sb.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null, matrix);

        SilkyUI.TransformMatrix = matrix;
        SilkyUI.Draw(Main.gameTimeCache, Main.spriteBatch);
        return true;
    }
}
