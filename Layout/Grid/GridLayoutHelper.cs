namespace SilkyUIFramework.Layout.Grid;

/// <summary>
/// Grid 布局的无状态辅助方法，包含轨道求和和偏移计算。
/// </summary>
public static class GridLayoutHelper
{
    /// <summary>
    /// 汇总整组轨道尺寸，并加上轨道之间的 fence gap。
    /// </summary>
    public static float SumTracks(IReadOnlyList<GridTrackOutput> tracks, float gap)
    {
        if (tracks.Count == 0) return 0f;

        var total = (tracks.Count - 1) * gap;
        for (var i = 0; i < tracks.Count; i++)
        {
            total += tracks[i].Size;
        }

        return total;
    }

    /// <summary>
    /// 汇总指定 Grid 区域覆盖的轨道尺寸，并加上区域内部的 gap。
    /// </summary>
    public static float SumTracks(IReadOnlyList<GridTrackOutput> tracks, int start, int span, float gap)
    {
        if (tracks.Count == 0) return 0f;

        start = Math.Clamp(start, 0, tracks.Count - 1);
        span = Math.Max(1, Math.Min(span, tracks.Count - start));

        var total = (span - 1) * gap;
        for (var i = start; i < start + span; i++)
        {
            total += tracks[i].Size;
        }

        return total;
    }

    /// <summary>
    /// 根据轨道最终尺寸计算每条轨道相对容器 InnerBounds 的起始偏移。
    /// </summary>
    public static void UpdateOffsets(GridTrackOutput[] tracks, float gap)
    {
        var offset = 0f;
        for (var i = 0; i < tracks.Length; i++)
        {
            tracks[i].Offset = offset;
            offset += tracks[i].Size + gap;
        }
    }
}
