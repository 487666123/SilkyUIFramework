namespace SilkyUIFramework.Helper;

public enum BlurMixingNumber
{
    One,
    Two,
    Three,
    Four,
    Five
}

/// <summary>
/// 实现模糊效果的帮助类
/// </summary>
public static class BlurHelper
{
    /// <summary>
    /// 把指定源 RenderTarget 绘制到指定 RenderTarget 并进行模糊 (batch 应处于关闭状态)
    /// </summary>
    public static void KawaseBlur(RenderTarget2D sourceRenderTarget, RenderTarget2D BlurRenderTarget,
        int blurIterationCount, float iterationOffsetMultiplier, float blurZoomMultiplierDenominator,
        BlurMixingNumber blurMixingNumber)
    {
        var batch = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        var original = device.GetRenderTargets();
        device.SetRenderTarget(BlurRenderTarget);

        batch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.Identity);
        batch.Draw(sourceRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero,
            BlurRenderTarget.Size() / Main.screenTarget.Size(), 0, 0f);
        batch.End();

        device.RestoreRenderTargets(original);

        KawaseBlur(BlurRenderTarget, blurIterationCount, iterationOffsetMultiplier, blurMixingNumber);
    }

    private static float[] GenerateGeometricSequence(int n, float radio)
    {
        if (n <= 0) return [];

        var result = new float[n];
        result[0] = 1;

        for (var i = 1; i < n; i++)
        {
            result[i] = result[i - 1] * radio;
        }

        return result;
    }

    private static void KawaseBlur(RenderTarget2D renderTarget,
        int iterationCount, float radio, BlurMixingNumber blurType = BlurMixingNumber.Three)
    {
        KawaseBlur(renderTarget, GenerateGeometricSequence(iterationCount, radio), blurType);
    }

    private static void KawaseBlur(RenderTarget2D renderTarget,
        float[] offsets, BlurMixingNumber blurType = BlurMixingNumber.Three)
    {
        if (offsets.Length == 0) return;
        if (SilkyUISystem.Instance.AssetProvider?.BlurEffect?.Value is not { } effect) return;

        var device = Main.graphics.GraphicsDevice;
        var batch = Main.spriteBatch;

        var original = device.GetRenderTargets();

        var renderTargetPool = SilkyUISystem.ServiceProvider.GetRequiredService<RenderTargetPool>();
        var renderTargetSwap = renderTargetPool.Rent(renderTarget.Width, renderTarget.Height);

        effect.Parameters["uPixelSize"]
            .SetValue(Vector2.One / new Vector2(renderTarget.Width, renderTarget.Height));

        SelectBlurEffectPasses(blurType, out var blurX, out var blurY);

        device.Viewport = new Viewport(0, 0, renderTarget.Width, renderTarget.Height);
        batch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Matrix.Identity);

        var offsetsSpan = offsets.AsSpan();
        foreach (var offset in offsetsSpan)
        {
            device.SetRenderTarget(renderTargetSwap);

            effect.Parameters["uBlurRadius"].SetValue(offset);
            blurX.Apply();
            batch.Draw(renderTarget, Vector2.Zero, null, Color.White);

            device.SetRenderTarget(renderTarget);

            blurY.Apply();
            batch.Draw(renderTargetSwap, Vector2.Zero, null, Color.White);
        }

        batch.End();

        device.RestoreRenderTargets(original);

        renderTargetPool.Return(renderTargetSwap);
    }

    private static void SelectBlurEffectPasses(BlurMixingNumber blurType, out EffectPass blurX, out EffectPass blurY)
    {
        var effect = SilkyUISystem.Instance.AssetProvider.BlurEffect.Value;
        switch (blurType)
        {
            default:
            case BlurMixingNumber.One:
                blurX = effect.CurrentTechnique.Passes["BlurX1"];
                blurY = effect.CurrentTechnique.Passes["BlurY1"];
                break;
            case BlurMixingNumber.Two:
                blurX = effect.CurrentTechnique.Passes["BlurX2"];
                blurY = effect.CurrentTechnique.Passes["BlurY2"];
                break;
            case BlurMixingNumber.Three:
                blurX = effect.CurrentTechnique.Passes["BlurX3"];
                blurY = effect.CurrentTechnique.Passes["BlurY3"];
                break;
            case BlurMixingNumber.Four:
                blurX = effect.CurrentTechnique.Passes["BlurX4"];
                blurY = effect.CurrentTechnique.Passes["BlurY4"];
                break;
            case BlurMixingNumber.Five:
                blurX = effect.CurrentTechnique.Passes["BlurX5"];
                blurY = effect.CurrentTechnique.Passes["BlurY5"];
                break;
        }
    }
}