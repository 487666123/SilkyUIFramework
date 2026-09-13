namespace SilkyUIFramework.Helper;

public enum BlurSampleCount { One, Two, Three, Four, Five }

/// <summary>
/// 提供背景图复制、模糊偏移生成以及横向和纵向模糊处理功能。
/// </summary>
public static class BlurHelper
{
    /// <summary>
    /// 将一个或多个源纹理合成为降采样目标纹理，并执行配置的多轮双向模糊。
    /// 调用此方法前，SpriteBatch 必须处于结束状态；目标纹理的尺寸决定最终的降采样分辨率。
    /// </summary>
    public static void Apply(ReadOnlySpan<RenderTarget2D> sources, RenderTarget2D destination,
        int iterationCount, float offsetMultiplier, BlurSampleCount sampleCount)
    {
        CopySourcesToTarget(sources, destination);
        var offsets = CreateOffsets(iterationCount, offsetMultiplier);
        ApplyBlurPasses(destination, offsets, sampleCount);
    }

    /// <summary>
    /// 将多个源纹理按目标尺寸缩放并绘制到目标纹理，为后续模糊处理准备输入。
    /// </summary>
    private static void CopySourcesToTarget(ReadOnlySpan<RenderTarget2D> sources, RenderTarget2D destination)
    {
        var batch = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        var original = device.GetRenderTargets();
        var originalViewport = device.Viewport;
        device.SetRenderTarget(destination);

        batch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, null, Matrix.Identity);
        foreach (var source in sources)
        {
            batch.Draw(source, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, destination.SizeVec2 / source.SizeVec2, 0, 0f);
        }
        batch.End();

        device.RestoreRenderTargets(original);
        device.Viewport = originalViewport;
    }

    /// <summary>
    /// 根据初始偏移量和倍率，生成每轮模糊使用的偏移量序列。
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
    /// 使用交换纹理逐轮执行横向和纵向模糊，并将最终结果保留在目标纹理中。
    /// </summary>
    private static void ApplyBlurPasses(RenderTarget2D blurTarget, ReadOnlySpan<float> offsets, BlurSampleCount sampleCount)
    {
        if (offsets.Length == 0) return;

        var effect = ModAsset.BlurEffect.Value;
        if (effect == null) return;

        var device = Main.graphics.GraphicsDevice;
        var batch = Main.spriteBatch;

        var original = device.GetRenderTargets();
        var originalViewport = device.Viewport;

        var renderTargetPool = RenderTargetPool.Instance;
        var swapTarget = renderTargetPool.Rent(blurTarget.Width, blurTarget.Height);

        effect.Parameters["uPixelSize"].SetValue(Vector2.One / blurTarget.SizeVec2);

        GetBlurPasses(sampleCount, out var horizontal, out var vertical);

        foreach (var offset in offsets)
        {
            effect.Parameters["uBlurRadius"].SetValue(offset);

            device.SetRenderTarget(swapTarget);

            batch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointWrap, null, null, null, Matrix.Identity);
            horizontal.Apply();
            batch.Draw(blurTarget, Vector2.Zero, null, Color.White);
            batch.End();

            device.SetRenderTarget(blurTarget);

            batch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointWrap, null, null, null, Matrix.Identity);
            vertical.Apply();
            batch.Draw(swapTarget, Vector2.Zero, null, Color.White);
            batch.End();
        }

        device.RestoreRenderTargets(original);
        device.Viewport = originalViewport;
        renderTargetPool.Return(swapTarget);
    }

    /// <summary>
    /// 根据采样数量选择对应的横向和纵向模糊着色器通道。
    /// </summary>
    private static void GetBlurPasses(BlurSampleCount sampleCount, out EffectPass horizontal, out EffectPass vertical)
    {
        var effect = ModAsset.BlurEffect.Value;
        var suffix = sampleCount switch
        {
            BlurSampleCount.Two => "2",
            BlurSampleCount.Three => "3",
            BlurSampleCount.Four => "4",
            BlurSampleCount.Five => "5",
            _ => "1"
        };

        horizontal = effect.CurrentTechnique.Passes["BlurX" + suffix];
        vertical = effect.CurrentTechnique.Passes["BlurY" + suffix];
    }
}
