﻿namespace SilkyUIFramework.BasicComponents;

public class SUICross(Color backgroundColor, Color borderColor) : UIView
{
    public float
        CrossSize = 24f,
        CrossRounded = 4,
        CrossBorder = 2;

    public Color
        CrossBorderColor = borderColor,
        CrossBorderHoverColor = borderColor,
        CrossBackgroundColor = backgroundColor,
        CrossBackgroundHoverColor = backgroundColor;

    public Vector2 CrossOffset = Vector2.Zero;


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