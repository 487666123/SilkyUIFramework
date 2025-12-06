namespace SilkyUIFramework.Elements;

public class SUIGif : UIView
{
    #region GifRenderer

    public delegate void GifRendererChangeEventHandler(SUIGif sender, GifRenderer newTexture2D, GifRenderer oldTexture2D);

    public GifRenderer GifRenderer
    {
        get;
        set
        {
            // ArgumentNullException.ThrowIfNull(value);
            OnTextureChanged(this, value, field);
            field = value;
        }
    }

    public Vector2 ImageOriginalSize => GifRenderer is null ? Vector2.Zero : GifRenderer.Size;

    public event GifRendererChangeEventHandler GifRendererChanged;

    protected virtual void OnTextureChanged(SUIGif sender, GifRenderer newTexture2D, GifRenderer oldTexture2D) =>
        GifRendererChanged?.Invoke(sender, newTexture2D, oldTexture2D);

    #endregion

    public Vector2 ImageOffset = Vector2.Zero;
    public Vector2 ImagePercent = Vector2.Zero;
    public Vector2 ImageAlign = Vector2.Zero;
    public Vector2 ImageOriginPercent = Vector2.Zero;

    /// <summary> 图片缩放 </summary>
    public Vector2 ImageScale { get; set; } = Vector2.One;

    /// <summary> 图片染色 </summary>
    public Color ImageColor { get; set; } = Color.White;

    public Rectangle? SourceRectangle { get; set; }

    public SUIGif(GifRenderer gifRenderer)
    {
        // ArgumentNullException.ThrowIfNull(gifRenderer);

        GifRenderer = gifRenderer;
        FitWidth = true;
        FitHeight = true;
    }

    public override void PreMeasure(float? width, float? height)
    {
        base.PreMeasure(width, height);

        if (GifRenderer == null) return;

        if (FitWidth)
        {
            SetInnerBoundsWidthRaw(GifRenderer.Width);
        }

        if (FitHeight)
        {
            SetInnerBoundsWidthRaw(GifRenderer.Height);
        }
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        if (GifRenderer is null) return;

        var position = InnerBounds.Position;
        var size = (Vector2)InnerBounds.Size;

        var imageOriginalSize = ImageOriginalSize;
        var completeOffset = ImageOffset + size * ImagePercent + (size - imageOriginalSize * ImageScale) * ImageAlign;

        GifRenderer.Update(gameTime);
        // 现在只给了位置，其他都还没做适配
        GifRenderer.Draw(spriteBatch, position);
    }
}