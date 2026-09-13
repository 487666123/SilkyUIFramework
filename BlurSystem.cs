namespace SilkyUIFramework;

/// <summary>
/// 管理背景模糊流程、界面背景捕获、渲染目标以及模糊参数配置。
/// </summary>
public class BlurSystem : ILoadable
{
    public static bool EnableBlur { get; set; } = true;

    /// <summary>
    /// 为每个窗口单独计算模糊结果。窗口重叠时，上层窗口可以看到下层已经模糊的画面。
    /// </summary>
    public static bool PerWindowBlur { get; set; } = false;

    /// <summary>
    /// 指示当前环境是否支持背景模糊。复古或迷幻效果启用时，背景模糊不可用。
    /// </summary>
    public static bool IsBlurAvailable => EnableBlur && Lighting.NotRetro;

    /// <summary> 背景模糊目标相对于源纹理的降采样因子。数值越大，目标纹理分辨率越低。 </summary>
    public static float BlurDownsampleFactor { get; set => field = Math.Max(value, 1f); } = 2f;

    /// <summary> 背景模糊的迭代次数。每次迭代依次执行横向和纵向模糊。 </summary>
    public static int BlurIterationCount { get; set; } = 3;

    /// <summary> 相邻两轮模糊之间的偏移量倍率。 </summary>
    public static float IterationOffsetMultiplier { get; set; } = 2f;

    /// <summary> 模糊着色器使用的采样数量。 </summary>
    public static BlurSampleCount BlurSampleCount { get; set; } = BlurSampleCount.Three;

    /// <summary> 存储背景模糊结果的降采样渲染目标。 </summary>
    public static RenderTarget2D BlurRenderTarget { get; private set; }

    /// <summary> 捕获窗口绘制前背景画面的全尺寸渲染目标。 </summary>
    public static RenderTarget2D InterfaceCaptureTarget { get; private set; }

    private static RenderTargetBinding[] _originalRenderTargetBindings;

    public void Load(Mod mod)
    {
        On_Main.DrawPlayerChatBubbles += static (orig, self) =>
        {
            if (IsBlurAvailable)
                PrepareRenderTargets();
            orig(self);
        };

        On_Main.DrawInterface += static (orig, self, gameTime) =>
        {
            if (IsBlurAvailable)
            {
                if (PerWindowBlur)
                {
                    orig(self, gameTime);
                    EndInterfaceCaptureAndComposite();
                    return;
                }

                ApplyBlur([Main.skyTarget, Main.screenTarget]);
            }

            orig(self, gameTime);
        };
    }

    public void Unload()
    {
        Main.RunOnMainThread(() =>
        {
            BlurRenderTarget?.Dispose();
            BlurRenderTarget = null;
            InterfaceCaptureTarget?.Dispose();
            InterfaceCaptureTarget = null;
        });
    }

    /// <summary>
    /// 根据当前屏幕尺寸和模糊模式准备背景模糊及窗口背景捕获所需的渲染目标。
    /// </summary>
    private static void PrepareRenderTargets()
    {
        var sourceWidth = Main.screenTarget.Width;
        var sourceHeight = Main.screenTarget.Height;

        var blurTargetWidth = (int)Math.Ceiling(sourceWidth / BlurDownsampleFactor);
        var blurTargetHeight = (int)Math.Ceiling(sourceHeight / BlurDownsampleFactor);

        BlurRenderTarget = GetOrRecreateTarget(BlurRenderTarget, blurTargetWidth, blurTargetHeight);

        _originalRenderTargetBindings = null;

        // 启用逐窗口模糊时，先捕获窗口绘制前的背景画面。
        if (PerWindowBlur)
        {
            // 切换到专用渲染目标，捕获窗口绘制前的背景画面。
            InterfaceCaptureTarget = GetOrRecreateTarget(InterfaceCaptureTarget, sourceWidth, sourceHeight);

            var batch = Main.spriteBatch;
            var device = Main.graphics.GraphicsDevice;

            batch.End();

            _originalRenderTargetBindings = device.GetRenderTargets();

            device.SetRenderTarget(InterfaceCaptureTarget);

            // 将当前游戏画面合成到背景捕获纹理中。
            batch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointWrap, null, null, null, Matrix.Identity);
            batch.Draw(Main.skyTarget, Vector2.Zero, null, Color.White);
            batch.Draw(Main.screenTarget, Vector2.Zero, null, Color.White);
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        }
    }

    /// <summary>
    /// 使用指定的源纹理和当前配置刷新背景模糊结果。调用此方法前，SpriteBatch 必须处于结束状态。
    /// </summary>
    public static void ApplyBlur(RenderTarget2D[] sources)
    {
        BlurHelper.Apply(sources, BlurRenderTarget,
            BlurIterationCount, IterationOffsetMultiplier, BlurSampleCount);
    }

    /// <summary>
    /// 恢复窗口背景捕获前的渲染目标，并将捕获的背景画面合成回原始目标。
    /// </summary>
    private static void EndInterfaceCaptureAndComposite()
    {
        if (_originalRenderTargetBindings is null) return;

        var batch = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        device.RestoreRenderTargets(_originalRenderTargetBindings);

        batch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.Identity);
        batch.Draw(InterfaceCaptureTarget, Vector2.Zero, null, Color.White);
        batch.End();
    }

    /// <summary>
    /// 返回指定尺寸的渲染目标；如果现有目标尺寸不匹配，则释放旧目标并创建新目标。
    /// </summary>
    private static RenderTarget2D GetOrRecreateTarget(RenderTarget2D target, int width, int height)
    {
        if (target is not null && target.Width == width && target.Height == height) return target;

        target?.Dispose();
        var graphicsDevice = Main.graphics.GraphicsDevice;

        return new RenderTarget2D(graphicsDevice, width, height, false,
            graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);
    }
}
