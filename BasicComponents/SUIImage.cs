using ReLogic.Content;

namespace SilkyUIFramework.BasicComponents;

[XmlElementMapping("Image")]
public class SUIImage : UIView
{
    #region Texture2D

    public delegate void TextureChangeEventHandler(SUIImage sender, Asset<Texture2D> newTexture2D, Asset<Texture2D> oldTexture2D);

    public Asset<Texture2D> Texture2D
    {
        get;
        set
        {
            if (value == field) return;
            field = value;

            if (FitWidth || FitHeight) MarkLayoutDirty();
            OnTextureChanged(this, value, field);
        }
    }

    public Vector2 ImageOriginalSize
    {
        get
        {
            if (Texture2D.Value is { } texture2D)
                return new(texture2D.Width, texture2D.Height);
            return Vector2.Zero;
        }
    }

    public event TextureChangeEventHandler TextureChanged;

    protected virtual void OnTextureChanged(SUIImage sender, Asset<Texture2D> newTexture2D, Asset<Texture2D> oldTexture2D) =>
        TextureChanged?.Invoke(sender, newTexture2D, oldTexture2D);

    #endregion

    public Vector2 ImageOffset { get; set; } = Vector2.Zero;
    public Vector2 ImagePercent { get; set; } = Vector2.Zero;
    public Vector2 ImageAlign { get; set; } = Vector2.Zero;
    public Vector2 ImageOriginPercent { get; set; } = Vector2.Zero;

    /// <summary> 图片缩放 </summary>
    public Vector2 ImageScale { get; set; } = Vector2.One;

    /// <summary> 图片染色 </summary>
    public Color ImageColor { get; set; } = Color.White;

    public Rectangle? SourceRectangle { get; set; }

    public SUIImage()
    {
        SetSize(30f, 30f);
        FitWidth = false;
        FitHeight = false;
    }

    public SUIImage(Asset<Texture2D> texture)
    {
        Texture2D = texture;
        FitWidth = true;
        FitHeight = true;
    }

    public override void Prepare(float? width, float? height)
    {
        base.Prepare(width, height);

        if (Texture2D == null || Texture2D.Value == null) return;

        if (FitWidth)
        {
            SetInnerBoundsWidth(Texture2D.Value.Width);
        }

        if (FitHeight)
        {
            SetInnerBoundsHeight(Texture2D.Value.Height);
        }
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);


        if (Texture2D?.Value == null) return;

        var position = InnerBounds.Position;
        var size = (Vector2)InnerBounds.Size;

        var imageOriginalSize = ImageOriginalSize;
        var completeOffset = ImageOffset + size * ImagePercent + (size - imageOriginalSize * ImageScale) * ImageAlign;

        spriteBatch.Draw(Texture2D.Value, position + completeOffset, SourceRectangle,
            ImageColor, 0f, imageOriginalSize * ImageOriginPercent, ImageScale, 0f, 0f);
    }
}