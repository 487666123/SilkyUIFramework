using Terraria.UI.Chat;

namespace SilkyUIFramework;

public class CursorSnippet : TextSnippet
{
    private readonly SUIEditText _editText;
    public DynamicSpriteFont Font { get; set; }

    public float TrueHeight { get; set; }

    public CursorSnippet(SUIEditText editText)
    {
        Text = " ";
        _editText = editText;
    }

    public override float GetStringLength(DynamicSpriteFont font) => 0f;

    public override bool UniqueDraw(bool justCheckingString,
        out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1)
    {
        if (Font == null)
        {
            size = Vector2.Zero;
            return true;
        }

        size = new Vector2(0, TrueHeight);

        if (!justCheckingString)
        {
            position.Y -= UITextView.GetFontOffset(Font) * scale;

            spriteBatch?.Draw(TextureAssets.MagicPixel.Value, position,
                new Rectangle(0, 0, 1, 1), _editText.CursorFlashColor, 0f, Vector2.Zero,
                new Vector2(2f, size.Y), SpriteEffects.None, 0f);
        }

        return true;
    }
}