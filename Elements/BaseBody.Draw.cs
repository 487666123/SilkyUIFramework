namespace SilkyUIFramework.Elements;

/// <summary>
/// BaseBody 绘制扩展。
/// 统一编排离屏合成、背景模糊与截图请求。
/// </summary>
public abstract partial class BaseBody
{
    /// <summary>
    /// 待处理截图请求标记：由 RequestScreenshot 设置，进入离屏分支时清除。
    /// </summary>
    private bool _captureRequested;

    /// <summary>
    /// 生成默认截图保存路径（我的文档\My Games\Terraria\tModLoader\{name}.png）。
    /// </summary>
    /// <param name="name">截图文件名（不含扩展名）。</param>
    /// <returns>完整 PNG 保存路径。</returns>
    public static string GetDefaultScreenshotPath(string name = "SilkyUI")
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string tmlPath = Path.Combine(docPath, "My Games", "Terraria", "tModLoader");
        return Path.Combine(tmlPath, $"{name}.png");
    }

    /// <summary>
    /// 截图输出路径。
    /// 调用 <see cref="RequestScreenshot"/> 前应确保路径有效且可写。
    /// </summary>
    public string ScreenshotSavePath { get; set; }

    /// <summary>
    /// 标记下一次可用离屏纹理时执行一次截图。
    /// </summary>
    public void RequestScreenshot() => _captureRequested = true;

    /// <summary>
    /// 是否启用离屏 RenderTarget 路径（常用于过渡动画）。
    /// 存在待处理截图请求时，即使为 <see langword="false"/> 也会强制离屏。
    /// </summary>
    public virtual bool UseRenderTarget { get; set; } = false;

    /// <summary>
    /// 离屏结果回绘到主目标时的透明度。
    /// 写入时自动夹紧到 [0, 1]。
    /// </summary>
    public virtual float Opacity
    {
        get;
        set => field = Math.Clamp(value, 0f, 1f);
    } = 1f;

    /// <summary>
    /// 离屏结果回绘到主目标时使用的变换矩阵。
    /// </summary>
    public Matrix RenderTargetMatrix = Matrix.CreateScale(1f, 1f, 1f);

    /// <summary>
    /// 是否在主体绘制前执行背景模糊采样。
    /// </summary>
    public virtual bool EnableBlur { get; set; } = false;

    /// <summary>
    /// 参与模糊采样的元素集合。默认仅采样当前容器。
    /// </summary>
    public virtual IEnumerable<UIView> BlurElements => [this];

    /// <summary>
    /// 绘制入口：根据配置与截图请求选择直绘或离屏绘制。
    /// </summary>
    /// <param name="gameTime">当前游戏时间。</param>
    /// <param name="spriteBatch">当前绘制批次。</param>
    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (UseRenderTarget || _captureRequested)
        {
            DrawWithRenderTarget(gameTime, spriteBatch);
            return;
        }

        DrawBodyCore(gameTime, spriteBatch);
    }

    /// <summary>
    /// 执行离屏绘制流程：先离屏绘制，再按矩阵与透明度合成回主目标。
    /// </summary>
    /// <param name="gameTime">当前游戏时间。</param>
    /// <param name="spriteBatch">当前绘制批次。</param>
    protected virtual void DrawWithRenderTarget(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!UseRenderTarget && !_captureRequested)
        {
            DrawBodyCore(gameTime, spriteBatch);
            return;
        }

        var captureScreenshot = _captureRequested;
        var device = Main.graphics.GraphicsDevice;
        var renderTargetPool = SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>();

        var rect = TransformBoundsToClipping(OuterBounds, SilkyUI.TransformMatrix);
        var renderTarget = renderTargetPool.Rent(rect.Width, rect.Height);

        //var backBufferWidth = device.PresentationParameters.BackBufferWidth;
        //var backBufferHeight = device.PresentationParameters.BackBufferHeight;
        //var renderTarget = renderTargetPool.Rent(backBufferWidth, backBufferHeight);

        try
        {
            // 切换渲染目标前先结束当前批次，避免 SpriteBatch 状态污染。
            spriteBatch.End();

            var original = device.GetRenderTargets();
            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);

            device.Viewport = new Viewport(-rect.X, -rect.Y, rect.Right, rect.Bottom);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, SilkyUI.ScissorRasterizerState, null, SilkyUI.TransformMatrix);

            DrawBodyCore(gameTime, spriteBatch);
            spriteBatch.End();

            device.RestoreRenderTargets(original);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null,
                SilkyUI.ScissorRasterizerState, null, RenderTargetMatrix);
            spriteBatch.Draw(renderTarget, new Vector2(rect.X, rect.Y), null, Color.White * Opacity, 0f, Vector2.Zero, Vector2.One, 0, 0);

            // 保存截图
            if (captureScreenshot) SaveScreenshot(renderTarget);
        }
        finally
        {
            renderTargetPool.Return(renderTarget);
            _captureRequested = false;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        }
    }

    /// <summary>
    /// 主体绘制钩子。默认走基类容器绘制逻辑，可由子类重写。
    /// </summary>
    protected virtual void DrawBodyCore(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
    }

    /// <summary>
    /// 将 RenderTarget 内容保存为 PNG，并在游戏内输出保存路径。
    /// </summary>
    /// <param name="renderTarget">待保存的离屏纹理。</param>
    private void SaveScreenshot(RenderTarget2D renderTarget)
    {
        var savePath = ScreenshotSavePath;

        using var stream = new FileStream(savePath, FileMode.Create);
        renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
        Main.NewText($"Screenshot Save Path: {savePath}");
    }

    /// <summary>
    /// 绘制当前元素；在需要时先执行模糊预处理与采样。
    /// </summary>
    /// <param name="gameTime">当前游戏时间。</param>
    /// <param name="spriteBatch">当前绘制批次。</param>
    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (EnableBlur && BlurMakeSystem.BlurAvailable && !Main.gameMenu)
        {
            if (BlurMakeSystem.SingleBlur)
            {
                spriteBatch.End();
                BlurMakeSystem.RefreshBlur([BlurMakeSystem.UserInterfaceRenderTarget]);
                spriteBatch.Begin(0, null, null, null, SilkyUI.ScissorRasterizerState, null, SilkyUI.TransformMatrix);
            }
            DrawBlurRegions();
        }

        base.Draw(gameTime, spriteBatch);
    }

    /// <summary>
    /// 对 <see cref="BlurElements"/> 对应区域执行模糊采样。
    /// </summary>
    public virtual void DrawBlurRegions()
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
