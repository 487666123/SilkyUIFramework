namespace SilkyUIFramework.Elements;

public abstract partial class BaseBody
{
    /// <summary>
    /// 我的文档\My Games\Terraria\tModLoader\SilkyUI.png
    /// </summary>
    /// <returns></returns>
    public static string GetDefaultScreenshotPath(string name = "SilkyUI")
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string tmlPath = Path.Combine(docPath, "My Games", "Terraria", "tModLoader");
        return Path.Combine(tmlPath, $"{name}.png");
    }

    private bool _capture = false;

    /// <summary>
    /// 截图保存路径，默认为
    /// </summary>
    public string ScreenshotSavePath { get; set; }

    /// <summary>
    /// 截图
    /// </summary>
    public void Catch() => _capture = true;

    /// <summary>
    /// 使用 RenderTarget2D 捕获 UI (可用于制作动画)
    /// </summary>
    public virtual bool UseRenderTarget { get; set; } = false;

    /// <summary>
    /// RenderTarget2D 透明度
    /// </summary>
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
        var render = renderTargetPool.Rent(backBufferWidth, backBufferHeight);

        RuntimeSafeHelper.SafeInvoke(delegate
        {
            spriteBatch.End();

            var original = device.GetRenderTargets();
            device.SetRenderTarget(render);
            device.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null,
                SilkyUI.TransformMatrix);

            base.HandleDraw(gameTime, spriteBatch);
            spriteBatch.End();
            device.RestoreRenderTargets(original);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.RasterizerStateForOverflowHidden, null,
                RenderTargetMatrix);
            spriteBatch.Draw(render, Vector2.Zero, null, Color.White * Opacity, 0f, Vector2.Zero, Vector2.One,
                0, 0);

            // 截图
            if (_capture)
            {
                var savePath = ScreenshotSavePath;

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    render.SaveAsPng(stream, render.Width, render.Height);
                    // 截图成功时提示
                    Main.NewText($"ScreenshotSavePath Save Path: {savePath}");
                }

                _capture = false;
            }
        });

        renderTargetPool.Return(render);
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