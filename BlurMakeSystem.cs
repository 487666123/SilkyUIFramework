namespace SilkyUIFramework;

/// <summary>
/// 调度背景模糊与界面捕获，管理所需的渲染目标和模糊配置。
/// </summary>
public class BlurMakeSystem : ILoadable
{
    public static bool EnableBlur { get; internal set; } = true;

    /// <summary>
    /// 每个 UI 单独计算模糊，使用捕获的界面画面作为输入。
    /// </summary>
    public static bool SingleBlur { get; internal set; } = false;

    /// <summary>
    /// 开启复古和迷幻效果后将不可用。
    /// </summary>
    public static bool BlurAvailable => EnableBlur && Lighting.NotRetro;

    /// <summary> 模糊纹理尺寸的降采样除数。 </summary>
    public static float BlurZoomMultiplierDenominator
    {
        get; internal set => field = Math.Max(value, 1f);
    } = 2f;

    /// <summary> 模糊迭代次数。 </summary>
    public static int BlurIterationCount { get; internal set; } = 3;

    /// <summary> 相邻迭代的偏移倍率。 </summary>
    public static float IterationOffsetMultiplier { get; internal set; } = 2f;

    /// <summary> 模糊混合档位。 </summary>
    public static BlurMixingNumber BlurMixingNumber { get; internal set; } = BlurMixingNumber.Three;

    /// <summary> 存储模糊结果的降采样纹理。 </summary>
    public static RenderTarget2D BlurRenderTarget { get; private set; }

    /// <summary> 捕获游戏画面和后续界面绘制的全尺寸纹理。 </summary>
    public static RenderTarget2D UserInterfaceRenderTarget { get; private set; }

    private static RenderTargetBinding[] _originalRenderTargetBindings;

    public void Load(Mod mod)
    {
        On_Main.DrawPlayerChatBubbles += static (orig, self) =>
        {
            if (BlurAvailable)
            {
                EnsureRenderTargets();

                if (SingleBlur)
                {
                    BeginInterfaceCapture();
                }
            }

            orig(self);
        };

        On_Main.DrawInterface += static (orig, self, gameTime) =>
        {
            if (!BlurAvailable)
            {
                orig(self, gameTime);
                return;
            }

            if (!SingleBlur)
            {
                RefreshBlur([Main.skyTarget, Main.screenTarget]);
                orig(self, gameTime);
                return;
            }

            orig(self, gameTime);
            EndInterfaceCapture();
        };
    }

    public void Unload()
    {
        Main.RunOnMainThread(() =>
        {
            BlurRenderTarget?.Dispose();
            BlurRenderTarget = null;
            UserInterfaceRenderTarget?.Dispose();
            UserInterfaceRenderTarget = null;
        });
    }

    /// <summary>
    /// 根据屏幕尺寸和当前模式准备模糊与界面捕获纹理。
    /// </summary>
    private static void EnsureRenderTargets()
    {
        var sourceWidth = Main.screenTarget.Width;
        var sourceHeight = Main.screenTarget.Height;

        var blurTargetWidth = (int)Math.Ceiling(sourceWidth / BlurZoomMultiplierDenominator);
        var blurTargetHeight = (int)Math.Ceiling(sourceHeight / BlurZoomMultiplierDenominator);

        BlurRenderTarget = EnsureTargetSize(BlurRenderTarget, blurTargetWidth, blurTargetHeight);

        if (SingleBlur)
        {
            UserInterfaceRenderTarget = EnsureTargetSize(UserInterfaceRenderTarget, sourceWidth, sourceHeight);
        }
    }

    /// <summary>
    /// 切换到界面捕获目标，复制游戏画面并保留批次供后续绘制使用。
    /// </summary>
    private static void BeginInterfaceCapture()
    {
        var batch = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        batch.End();
        _originalRenderTargetBindings = device.GetRenderTargets();

        device.SetRenderTarget(UserInterfaceRenderTarget);

        batch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.Identity);
        batch.Draw(Main.skyTarget, Vector2.Zero, null, Color.White);
        batch.Draw(Main.screenTarget, Vector2.Zero, null, Color.White);
    }

    /// <summary>
    /// 使用指定画面和当前配置刷新模糊结果。调用前绘制批次应处于关闭状态。
    /// </summary>
    public static void RefreshBlur(RenderTarget2D[] source)
    {
        BlurHelper.Apply(source, BlurRenderTarget,
            BlurIterationCount, IterationOffsetMultiplier, BlurMixingNumber);
    }

    /// <summary>
    /// 恢复捕获前的目标，并合成捕获的完整画面。
    /// </summary>
    private static void EndInterfaceCapture()
    {
        var batch = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        device.RestoreRenderTargets(_originalRenderTargetBindings);

        batch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.Identity);
        batch.Draw(UserInterfaceRenderTarget, Vector2.Zero, null, Color.White);
        batch.End();
    }

    /// <summary>
    /// 判断 RenderTarget2D 是不是指定的尺寸，不是就释放，同时创建一个新的 RenderTarget2D
    /// </summary>
    private static RenderTarget2D EnsureTargetSize(RenderTarget2D target, int width, int height)
    {
        if (target is not null &&
            target.Width == width &&
            target.Height == height) return target;

        target?.Dispose();
        var graphicsDevice = Main.graphics.GraphicsDevice;

        return new RenderTarget2D(graphicsDevice, width, height, false,
            graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);
    }
}
