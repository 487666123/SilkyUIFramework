namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>有视框的不可变矢量图标。图层按从后到前的顺序排列。</summary>
public sealed class SvgIcon
{
    private readonly SvgIconLayer[] _layers;

    /// <summary>视框的 X、Y、宽度和高度，单位为路径坐标。</summary>
    public Vector4 ViewBox { get; }

    /// <summary>图标的有序图层。</summary>
    public ReadOnlySpan<SvgIconLayer> Layers => _layers;

    /// <summary>构建图标并复制图层集合。</summary>
    /// <param name="viewBox">视框的 X、Y、宽度和高度。</param>
    /// <param name="layers">从后到前绘制的图层。</param>
    public SvgIcon(Vector4 viewBox, IEnumerable<SvgIconLayer> layers)
    {
        ValidateViewBox(viewBox);
        ArgumentNullException.ThrowIfNull(layers);
        _layers = layers.ToArray();
        if (Array.Exists(_layers, layer => layer.Path is null))
            throw new ArgumentException("图标图层必须提供路径。", nameof(layers));
        ViewBox = viewBox;
    }

    internal static void ValidateViewBox(Vector4 viewBox)
    {
        if (!float.IsFinite(viewBox.X) || !float.IsFinite(viewBox.Y) || !float.IsFinite(viewBox.Z) ||
            !float.IsFinite(viewBox.W) || viewBox.Z <= 0f || viewBox.W <= 0f)
            throw new ArgumentOutOfRangeException(nameof(viewBox), "视框必须有限，且宽高为正数。");
    }
}

/// <summary>一个填充或描边图层，颜色采用 SilkyUI 的预乘 Alpha 约定。</summary>
/// <param name="Path">填充或描边使用的矢量路径。</param>
/// <param name="Color">纯色，或纵向渐变在视框顶部的颜色。</param>
/// <param name="Stroke">描边样式。为空时填充路径。</param>
/// <param name="BottomColor">纵向渐变在视框底部的颜色。为空时使用纯色。</param>
public readonly record struct SvgIconLayer(VectorPath Path, Color Color, PathStroke Stroke = null, Color? BottomColor = null);
