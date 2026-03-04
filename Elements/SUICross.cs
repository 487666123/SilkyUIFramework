namespace SilkyUIFramework.Elements;

public struct CrossStyle
{
    public Size Size;
    public float Border, BorderRadius;
    public Color BorderColor, BackgroundColor;
}

[XmlElementMapping("Cross")]
public class SUICross : UIView
{
    private CrossStyle _style = new()
    {
        Size = new Size(24f),
        BorderRadius = 4f,
        Border = 2f
    };

    public Size CrossSize { get => _style.Size; set => _style.Size = value; }
    public float CrossBorderRadius { get => _style.BorderRadius; set => _style.BorderRadius = value; }
    public float CrossBorder { get => _style.Border; set => _style.Border = value; }

    public Color CrossBorderColor { get => _style.BorderColor; set => _style.BorderColor = value; }
    public Color CrossBackgroundColor { get => _style.BackgroundColor; set => _style.BackgroundColor = value; }

    public Anchor CrossLeft = new(0, 0, 0.5f);
    public Anchor CrossTop = new(0, 0, 0.5f);

    public override void OnMouseEnter(UIMouseEvent evt)
    {
        base.OnMouseEnter(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        var left = CrossLeft.CalculatePosition(InnerBounds.Width, _style.Size.Width);
        var top = CrossLeft.CalculatePosition(InnerBounds.Height, _style.Size.Height);

        var position = InnerBounds.Position + new Vector2(left, top);

        SDFGraphics.HasBorderCross(position, _style.Size, _style.BorderRadius,
            _style.BackgroundColor, _style.Border, _style.BorderColor, SilkyUI.TransformMatrix);
    }
}
