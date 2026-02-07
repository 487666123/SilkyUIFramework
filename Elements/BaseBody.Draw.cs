namespace SilkyUIFramework.Elements;

public abstract partial class BaseBody
{
    private bool _capture = false;

    public void Catch() => _capture = true;

    public virtual bool UseRenderTarget { get; set; } = false;

    public virtual float Opacity
    {
        get;
        set => field = Math.Clamp(value, 0f, 1f);
    } = 1f;

    public Matrix RenderTargetMatrix = Matrix.CreateScale(1f, 1f, 1f);

    public virtual bool EnableBlur { get; set; } = false;
    public virtual IEnumerable<UIView> BlurElements => [this];

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (UseRenderTarget || _capture) UseRenderTargetDraw(gameTime, spriteBatch);
        else base.HandleDraw(gameTime, spriteBatch);
    }

    protected virtual void UseRenderTargetDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var device = Main.graphics.GraphicsDevice;

        var backBufferWidth = device.PresentationParameters.BackBufferWidth;
        var backBufferHeight = device.PresentationParameters.BackBufferHeight;
        var renderTargetPool = SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>();
        var uiRenderTarget = renderTargetPool.Rent(backBufferWidth, backBufferHeight);

        RuntimeSafeHelper.SafeInvoke(delegate
        {
            spriteBatch.End();

            var original = device.GetRenderTargets();
            device.SetRenderTarget(uiRenderTarget);
            device.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null,
                SilkyUI.TransformMatrix);

            base.HandleDraw(gameTime, spriteBatch);
            spriteBatch.End();
            device.RestoreRenderTargets(original);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null,
                RenderTargetMatrix);
            spriteBatch.Draw(uiRenderTarget, Vector2.Zero, null, Color.White * Opacity, 0f, Vector2.Zero, Vector2.One,
                0, 0);

            if (_capture)
            {
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string tmlPath = Path.Combine(docPath, "My Games", "Terraria", "tModLoader");
                string savePath = Path.Combine(tmlPath, "SilkyUI.png");

                Main.NewText($"Save Path: {savePath}");

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    uiRenderTarget.SaveAsPng(stream, uiRenderTarget.Width, uiRenderTarget.Height);
                }

                _capture = false;
            }
        });

        renderTargetPool.Return(uiRenderTarget);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (EnableBlur && BlurMakeSystem.BlurAvailable && !Main.gameMenu)
        {
            if (BlurMakeSystem.SingleBlur)
            {
                spriteBatch.End();
                BlurMakeSystem.KawaseBlur();
                spriteBatch.Begin(0, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null,
                    SilkyUI.TransformMatrix);
            }

            DrawBlurRectangle();
        }

        base.Draw(gameTime, spriteBatch);
    }

    /// <summary>
    /// 绘制模糊矩形
    /// </summary>
    public virtual void DrawBlurRectangle()
    {
        if (BlurElements == null) return;

        var scale = Main.UIScale;

        foreach (var el in BlurElements.Where(el => !el.Invalid))
        {
            var bounds = el.Bounds;

            var position = bounds.Position * scale;
            var size = bounds.Size * scale;

            var borderRadius = el.BorderRadius * scale;

            SDFRectangle.SampleVersion(BlurMakeSystem.BlurRenderTarget, position, size, borderRadius, Matrix.Identity);
        }
    }
}