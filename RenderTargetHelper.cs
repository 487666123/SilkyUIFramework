﻿namespace SilkyUIFramework;

public static class RenderTargetHelper
{
    /// <summary>
    /// 记录原来 RenderTargetUsage, 并全部设为 PreserveContents 防止设置新的 RenderTarget 时候消失
    /// </summary>
    public static Dictionary<RenderTargetBinding, RenderTargetUsage?> RecordUsage(this RenderTargetBinding[] bindings)
    {
        if (bindings is null || bindings.Length == 0) return null;

        var rtUsageRecords = new Dictionary<RenderTargetBinding, RenderTargetUsage?>();
        foreach (var item in bindings)
        {
            if (item.RenderTarget is RenderTarget2D rt)
            {
                rtUsageRecords[item] = rt.RenderTargetUsage;
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            }
            else
            {
                rtUsageRecords[item] = null;
            }
        }

        return rtUsageRecords;
    }

    /// <summary>
    /// 恢复渲染目标使用情况
    /// </summary>
    public static void Restore(this Dictionary<RenderTargetBinding, RenderTargetUsage?> usages)
    {
        if (usages is null) return;

        foreach (var (key, value) in usages)
        {
            if (key.renderTarget is RenderTarget2D rt && value is RenderTargetUsage usage)
            {
                rt.RenderTargetUsage = usage;
            }
        }
    }
}
