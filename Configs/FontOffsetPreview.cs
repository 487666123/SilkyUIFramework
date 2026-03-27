using ReLogic.Content;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;

namespace SilkyUIFramework.Configs;

public class DeathTextOffsetPreview : FloatElement
{
    protected Asset<DynamicSpriteFont> SpriteFont;
    protected float TextScale = 1f;

    public override void OnBind()
    {
        base.OnBind();
        Height.Set(100f, 0);
        SpriteFont = FontAssets.DeathText;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        var dimensions = _dimensions;
        dimensions.X += 2;
        dimensions.Y += 35f;
        dimensions.Width -= 4;
        dimensions.Height -= 37f;

        var magicPixel = TextureAssets.MagicPixel;

        spriteBatch.Draw(magicPixel.Value, dimensions.Position(), new Rectangle(0, 0, 1, 1), Color.White, 0f, Vector2.Zero, new Vector2(dimensions.Width, 2), 0, 1);
        spriteBatch.Draw(magicPixel.Value, dimensions.Position() + new Vector2(0, 2), new Rectangle(0, 0, 1, 1), Color.White * 0.25f, 0f, Vector2.Zero, new Vector2(dimensions.Width, dimensions.Height - 4), 0, 1);
        spriteBatch.Draw(magicPixel.Value, dimensions.Position() + new Vector2(0, dimensions.Height - 2), new Rectangle(0, 0, 1, 1), Color.White, 0f, Vector2.Zero, new Vector2(dimensions.Width, 2), 0, 1);

        var stringSize = ChatManager.GetStringSize(SpriteFont.Value, "调整到你觉得顺眼的位置", Vector2.One) * TextScale;
        spriteBatch.DrawString(SpriteFont.Value, $"调整到你觉得顺眼的位置", dimensions.Center() + new Vector2(0f, (float)GetObject() * TextScale) - stringSize * new Vector2(0.5f, 0.5f), Color.White, 0f, Vector2.Zero, TextScale, 0, 0f);
    }

    public override void SetValue(object value) => base.SetValue(MathF.Round((float)value, 2));
}

public class MouseTextOffsetPreview : DeathTextOffsetPreview
{
    public override void OnBind()
    {
        base.OnBind();
        Height.Set(80f, 0);
        SpriteFont = FontAssets.MouseText;
    }
}