namespace SilkyUIFramework.Helper;

public enum BlurMixingNumber { One, Two, Three, Four, Five }

/// <summary>
/// 负责源图复制、模糊偏移生成和横纵向模糊处理。
/// </summary>
public static class BlurHelper
{
    /// <summary>
    /// 将源图复制到目标并执行模糊。调用前绘制批次应处于关闭状态。
    /// 降采样尺寸由目标纹理决定。
    /// </summary>
    public static void Apply(ReadOnlySpan<RenderTarget2D> sources, RenderTarget2D destination,
        int iterationCount, float offsetMultiplier, BlurMixingNumber mixingNumber)
    {
        CopySource(sources, destination);
        var offsets = CreateOffsets(iterationCount, offsetMultiplier);
        ApplyPasses(destination, offsets, mixingNumber);
    }

    /// <summary>
    /// 复制源图，为后续模糊准备目标纹理。
    /// </summary>
    private static void CopySource(ReadOnlySpan<RenderTarget2D> sources, RenderTarget2D destination)
    {
        var batch = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        var original = device.GetRenderTargets();
        var originalViewport = device.Viewport;
        device.SetRenderTarget(destination);

        batch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.Identity);
        foreach (var source in sources)
        {
            batch.Draw(source, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, destination.SizeVec2 / source.SizeVec2, 0, 0f);
        }
        batch.End();

        device.RestoreRenderTargets(original);
        device.Viewport = originalViewport;
    }

    /// <summary>
    /// 按等比数列生成每轮模糊使用的偏移量。
    /// </summary>
    private static float[] CreateOffsets(int iterationCount, float offsetMultiplier)
    {
        if (iterationCount <= 0) return [];

        var offsets = new float[iterationCount];
        offsets[0] = 1;

        for (var i = 1; i < iterationCount; i++)
        {
            offsets[i] = offsets[i - 1] * offsetMultiplier;
        }

        return offsets;
    }

    /// <summary>
    /// 使用交换纹理依次执行每轮横向和纵向模糊。
    /// </summary>
    private static void ApplyPasses(RenderTarget2D target, float[] offsets, BlurMixingNumber mixingNumber)
    {
        if (offsets.Length == 0) return;

        var effect = ModAsset.BlurEffect.Value;
        if (effect == null) return;

        var device = Main.graphics.GraphicsDevice;
        var batch = Main.spriteBatch;

        var original = device.GetRenderTargets();
        var originalViewport = device.Viewport;

        var renderTargetPool = SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>();
        var swapTarget = renderTargetPool.Rent(target.Width, target.Height);

        effect.Parameters["uPixelSize"]
            .SetValue(Vector2.One / new Vector2(target.Width, target.Height));

        GetPasses(mixingNumber, out var horizontal, out var vertical);

        device.Viewport = new Viewport(0, 0, target.Width, target.Height);
        batch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.Identity);

        foreach (var offset in offsets.AsSpan())
        {
            device.SetRenderTarget(swapTarget);

            effect.Parameters["uBlurRadius"].SetValue(offset);
            horizontal.Apply();
            batch.Draw(target, Vector2.Zero, null, Color.White);

            device.SetRenderTarget(target);

            vertical.Apply();
            batch.Draw(swapTarget, Vector2.Zero, null, Color.White);
        }

        batch.End();

        device.RestoreRenderTargets(original);
        device.Viewport = originalViewport;
        renderTargetPool.Return(swapTarget);
    }

    /// <summary>
    /// 根据混合档位选择横向和纵向着色器通道。
    /// </summary>
    private static void GetPasses(BlurMixingNumber mixingNumber,
        out EffectPass horizontal, out EffectPass vertical)
    {
        var effect = ModAsset.BlurEffect.Value;
        var suffix = mixingNumber switch
        {
            BlurMixingNumber.Two => "2",
            BlurMixingNumber.Three => "3",
            BlurMixingNumber.Four => "4",
            BlurMixingNumber.Five => "5",
            _ => "1"
        };

        horizontal = effect.CurrentTechnique.Passes["BlurX" + suffix];
        vertical = effect.CurrentTechnique.Passes["BlurY" + suffix];
    }
}
