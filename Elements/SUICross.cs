using SilkyUIFramework.StyleSystem;

namespace SilkyUIFramework.Elements;

[XmlElementMapping("Cross")]
public class SUICross : UIView
{
    public Size CrossSize { get; set; } = new Size(22f);
    public float CrossBorderRadius { get; set; } = 3.5f;
    public float CrossBorder { get; set; } = 2f;

    public Color CrossBorderColor { get; set; }
    public Color CrossBackgroundColor { get; set; }

    public Anchor CrossLeft = new(0, 0, 0.5f);
    public Anchor CrossTop = new(0, 0, 0.5f);

    public SUICross()
    {
        StyleSheet.AllTransition.Duration = 0.2f;

        StyleSheet.SetStyle(UIElementState.Normal, new StyleDefinition
        {
            [$"{nameof(CrossBorderColor)}"] = SUIColor.Border * 0.75f,
            [$"{nameof(CrossBackgroundColor)}"] = SUIColor.Warn * 0.75f,
        });

        StyleSheet.SetStyle(UIElementState.Hover, new StyleDefinition
        {
            [$"{nameof(CrossBorderColor)}"] = SUIColor.Highlight,
            [$"{nameof(CrossBackgroundColor)}"] = SUIColor.Warn,
        });
    }

    public override void OnMouseEnter(UIMouseEvent evt)
    {
        base.OnMouseEnter(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        var left = CrossLeft.CalculatePosition(InnerBounds.Width, CrossSize.Width);
        var top = CrossLeft.CalculatePosition(InnerBounds.Height, CrossSize.Height);

        var position = InnerBounds.Position + new Vector2(left, top);

        SDFGraphics.HasBorderCross(position, CrossSize, CrossBorderRadius,
            CrossBackgroundColor, CrossBorder, CrossBorderColor, SilkyUI.TransformMatrix);
    }
}
