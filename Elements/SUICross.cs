namespace SilkyUIFramework.Elements;

[XmlElementMapping("Cross")]
public class SUICross : UIView
{
    public float CrossSize { get; set; } = 24f;
    public float CrossRounded { get; set; } = 4;
    public float CrossBorder { get; set; } = 2;

    public Color CrossBorderColor { get; set; }
    public Color CrossBorderHoverColor { get; set; }
    public Color CrossBackgroundColor { get; set; }
    public Color CrossBackgroundHoverColor { get; set; }

    public Vector2 CrossOffset { get; set; } = Vector2.Zero;

    public SUICross() { }

    public SUICross(Color backgroundColor, Color borderColor)
    {
        CrossBorderColor = borderColor;
        CrossBorderHoverColor = borderColor;
        CrossBackgroundColor = backgroundColor;
        CrossBackgroundHoverColor = backgroundColor;
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        base.OnMouseLeave(evt);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        var position = InnerBounds.Position;
        var size = InnerBounds.Size;

        var crossBorderColor = HoverTimer.Lerp(CrossBorderColor, CrossBorderHoverColor);
        var crossBackground = HoverTimer.Lerp(CrossBackgroundColor, CrossBackgroundHoverColor);
        var crossPosition = position + (size - new Vector2(CrossSize)) / 2f + CrossOffset;

        SDFGraphics.HasBorderCross(crossPosition, CrossSize, CrossRounded,
            crossBackground, CrossBorder, crossBorderColor, SilkyUI.TransformMatrix);
    }
}