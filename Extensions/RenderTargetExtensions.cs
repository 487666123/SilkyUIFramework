namespace SilkyUIFramework.Extensions;

public static class RenderTargetExtensions
{
    extension(RenderTarget2D renderTarget)
    {
        public Vector2 SizeVec2 => new(renderTarget.Width, renderTarget.Height);
    }

    /// <summary>
    /// 恢复渲染目标<br/>
    /// 如若渲染目标的 <see cref="RenderTarget2D.RenderTargetUsage"/> 为 <see cref="RenderTargetUsage.DiscardContents"/><br/>
    /// 直接 <see cref="GraphicsDevice.SetRenderTargets(RenderTargetBinding[])"/> 会清空画布<br/>
    /// 使用此方法会先将 <see cref="RenderTarget2D.RenderTargetUsage"/> 设置为 <see cref="RenderTargetUsage.PreserveContents"/><br/>
    /// 再调用 <see cref="GraphicsDevice.SetRenderTargets(RenderTargetBinding[])"/>，从而避免清空画布<br/>
    /// 最后再将 <see cref="RenderTarget2D.RenderTargetUsage"/> 恢复为原来的值
    /// </summary>
    public static void RestoreRenderTargets(this GraphicsDevice device, RenderTargetBinding[] bindings)
    {
        if (bindings != null && bindings.Length > 0)
        {
            var original = bindings.GetUsage();
            bindings.SetUsage(RenderTargetUsage.PreserveContents);
            device.SetRenderTargets(bindings);
            bindings.SetUsage(original);
        }
        else
        {
            var original = device.PresentationParameters.RenderTargetUsage;
            device.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            device.SetRenderTarget(null);
            device.PresentationParameters.RenderTargetUsage = original;
        }
    }

    public static RenderTargetUsage?[] GetUsage(this RenderTargetBinding[] bindings)
    {
        if (bindings is null || bindings.Length == 0) return null;

        var span = bindings.AsSpan();
        var usages = new RenderTargetUsage?[span.Length];

        for (var i = 0; i < span.Length; i++)
        {
            if (span[i].RenderTarget is RenderTarget2D renderTarget)
                usages[i] = renderTarget.RenderTargetUsage;
        }

        return usages;
    }

    public static void SetUsage(this RenderTargetBinding[] bindings, RenderTargetUsage usage)
    {
        if (bindings is null || bindings.Length == 0) return;

        var span = bindings.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i].RenderTarget is RenderTarget2D renderTarget)
                renderTarget.RenderTargetUsage = usage;
        }
    }

    public static void SetUsage(this RenderTargetBinding[] bindings, RenderTargetUsage?[] usage)
    {
        if (bindings is null || usage is null || bindings.Length == 0 || usage.Length == 0 ||
            bindings.Length != usage.Length) return;

        var bindingsSpan = bindings.AsSpan();
        var usageSpan = usage.AsSpan();

        for (var i = 0; i < bindingsSpan.Length; i++)
        {
            if (bindingsSpan[i].RenderTarget is not RenderTarget2D renderTarget || usageSpan[i] is not { } renderTargetUsage) continue;
            renderTarget.RenderTargetUsage = renderTargetUsage;
        }

        return;
    }
}
