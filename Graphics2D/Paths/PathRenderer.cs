using System.Runtime.CompilerServices;

namespace SilkyUIFramework.Graphics2D.Paths;

/// <summary>准备并绘制二维路径。图形设备上的绘制器实例应在图形线程创建、复用和释放。</summary>
/// <remarks>绘制颜色采用 SilkyUI 的预乘 Alpha 约定。网格准备过程无需访问图形设备。</remarks>
public sealed class PathRenderer : IDisposable
{
    private readonly GraphicsDevice _device;

    private readonly BasicEffect _effect;

    private readonly ConditionalWeakTable<PathGradient, Texture2D> _gradientStops = new();

    private bool _disposed;

    /// <summary>为指定图形设备创建路径绘制器。</summary>
    /// <param name="device">用于提交网格的 FNA 图形设备。</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> 为 <see langword="null"/>。</exception>
    public PathRenderer(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _device = device;
        _effect = new BasicEffect(device) { VertexColorEnabled = true };
    }

    /// <summary>
    /// 把路径准备为当前比例下的网格。矩阵映射到视口内的像素坐标，pixelBounds 也使用该坐标。
    /// 反向填充以这个矩形为外边界。普通填充会隐式闭合所有轮廓。
    /// </summary>
    /// <param name="path">要填充的路径。</param>
    /// <param name="transform">将路径映射到视口内像素坐标的二维仿射变换。</param>
    /// <param name="pixelBounds">以视口内像素坐标表示的裁剪边界。</param>
    /// <param name="antialias">是否计算边缘的像素覆盖率。</param>
    /// <returns>可重复绘制的填充网格。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException">矩阵或变换后的坐标无效。</exception>
    public static PathMesh PrepareFill(VectorPath path, Matrix transform, Rectangle pixelBounds, bool antialias = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        VectorPathBuilder.CheckTransform(transform);
        if (pixelBounds.Width <= 0 || pixelBounds.Height <= 0) return new PathMesh([], [], transform);
        var tessellator = new PathTessellator(pixelBounds, path.FillRule, antialias);
        foreach (var contour in new PathFlattener(transform).Flatten(path))
            tessellator.AddContour(contour.Points.Select(point => point.Position), transform);
        return tessellator.Build(transform);
    }

    /// <summary>准备描边网格。缩放或旋转改变时应重新准备，描边宽度跟随变换。</summary>
    /// <param name="path">要描边的路径。</param>
    /// <param name="stroke">宽度、端帽、接缝和虚线样式。</param>
    /// <param name="transform">将路径映射到视口内像素坐标的二维仿射变换。</param>
    /// <param name="pixelBounds">以视口内像素坐标表示的裁剪边界。</param>
    /// <param name="antialias">是否计算边缘的像素覆盖率。</param>
    /// <returns>可重复绘制的描边网格。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> 或 <paramref name="stroke"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException">矩阵、变换后的坐标或虚线细分结果超出有效范围。</exception>
    public static PathMesh PrepareStroke(VectorPath path, PathStroke stroke, Matrix transform, Rectangle pixelBounds,
        bool antialias = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(stroke);
        VectorPathBuilder.CheckTransform(transform);
        if (pixelBounds.Width <= 0 || pixelBounds.Height <= 0) return new PathMesh([], [], transform);
        var contours = new PathFlattener(transform).Flatten(path);

        var tessellator = new PathTessellator(pixelBounds, PathFillRule.NonZero, antialias);

        var outlineTransform = stroke.Width == 0f ? Matrix.Identity : transform;
        foreach (var outline in new PathStroker(stroke, transform).Stroke(contours))
            tessellator.AddContour(outline, outlineTransform);
        return tessellator.Build(transform);
    }

    /// <summary>在 SilkyUI 的常规延迟批次内填充路径，使用当前 UI 变换和裁剪。</summary>
    /// <param name="spriteBatch">已经开始的 SilkyUI 延迟绘制批次。</param>
    /// <param name="path">要填充的路径。</param>
    /// <param name="color">预乘 Alpha 的填充颜色。</param>
    /// <param name="antialias">是否计算边缘的像素覆盖率。</param>
    public void Fill(SpriteBatch spriteBatch, VectorPath path, Color color, bool antialias = true)
    {
        var transform = GetUiTransform(spriteBatch);
        DrawInUI(spriteBatch, PrepareFill(path, transform, GetUiPixelBounds(), antialias), color);
    }

    /// <summary>在 SilkyUI 的常规延迟批次内绘制路径的描边。</summary>
    /// <param name="spriteBatch">已经开始的 SilkyUI 延迟绘制批次。</param>
    /// <param name="path">要描边的路径。</param>
    /// <param name="stroke">宽度、端帽、接缝和虚线样式。</param>
    /// <param name="color">预乘 Alpha 的描边颜色。</param>
    /// <param name="antialias">是否计算边缘的像素覆盖率。</param>
    public void Stroke(SpriteBatch spriteBatch, VectorPath path, PathStroke stroke, Color color, bool antialias = true)
    {
        var transform = GetUiTransform(spriteBatch);
        DrawInUI(spriteBatch, PrepareStroke(path, stroke, transform, GetUiPixelBounds(), antialias), color);
    }

    /// <summary>在 SilkyUI 的延迟批次中用渐变填充路径。</summary>
    /// <param name="spriteBatch">已经开始的 SilkyUI 延迟绘制批次。</param>
    /// <param name="path">要填充的路径。</param>
    /// <param name="gradient">路径局部坐标中的渐变画笔。</param>
    /// <param name="antialias">是否计算边缘像素覆盖率。</param>
    public void FillGradient(SpriteBatch spriteBatch, VectorPath path, PathGradient gradient, bool antialias = true)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        var transform = GetUiTransform(spriteBatch);
        DrawGradientInUI(spriteBatch, PrepareFill(path, transform, GetUiPixelBounds(), antialias), gradient);
    }

    /// <summary>在 SilkyUI 的延迟批次中用渐变绘制路径的描边。</summary>
    /// <param name="spriteBatch">已经开始的 SilkyUI 延迟绘制批次。</param>
    /// <param name="path">要描边的路径。</param>
    /// <param name="stroke">线宽、端帽、接缝和虚线样式。</param>
    /// <param name="gradient">路径局部坐标中的渐变画笔。</param>
    /// <param name="antialias">是否计算边缘像素覆盖率。</param>
    public void StrokeGradient(SpriteBatch spriteBatch, VectorPath path, PathStroke stroke, PathGradient gradient, bool antialias = true)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        var transform = GetUiTransform(spriteBatch);
        DrawGradientInUI(spriteBatch, PrepareStroke(path, stroke, transform, GetUiPixelBounds(), antialias), gradient);
    }

    /// <summary>在 SilkyUI 的常规延迟批次中绘制已准备好的网格，保留提交顺序和 UI 裁剪。</summary>
    /// <param name="spriteBatch">已经开始的 SilkyUI 延迟绘制批次。</param>
    /// <param name="mesh">与当前 UI 比例相符的预制网格。</param>
    /// <param name="color">预乘 Alpha 的绘制颜色。</param>
    /// <param name="offset">以设备像素为单位的额外平移。</param>
    /// <exception cref="ArgumentException">批次与绘制器使用了不同的图形设备。</exception>
    /// <exception cref="ObjectDisposedException">绘制器已释放。</exception>
    public void DrawInUI(SpriteBatch spriteBatch, PathMesh mesh, Color color, Point offset = default) =>
        DrawInUiCore(spriteBatch, mesh, color, null, offset);

    /// <summary>在 SilkyUI 的延迟批次中用渐变绘制预制网格。</summary>
    /// <param name="spriteBatch">已经开始的 SilkyUI 延迟绘制批次。</param>
    /// <param name="mesh">与当前 UI 比例相符的预制网格。</param>
    /// <param name="gradient">准备网格前的路径局部坐标中的渐变画笔。</param>
    /// <param name="offset">以设备像素为单位，同时平移网格和渐变。</param>
    public void DrawGradientInUI(SpriteBatch spriteBatch, PathMesh mesh, PathGradient gradient, Point offset = default)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        DrawInUiCore(spriteBatch, mesh, Color.White, gradient, offset);
    }

    private void DrawInUiCore(SpriteBatch spriteBatch, PathMesh mesh, Color color, PathGradient gradient, Point offset)
    {
        var transform = GetUiTransform(spriteBatch);
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.IsEmpty || color.A == 0) return;
        spriteBatch.End();
        try
        {
            DrawCore(mesh, color, gradient, offset, scissor: true, resumePass: spriteBatch.spriteEffectPass);
        }
        finally
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null,
                SilkyUI.ScissorRasterizerState, null, transform);
        }
    }

    /// <summary>
    /// 向已绑定的 FNA 渲染目标立即绘制网格。颜色遵循 SilkyUI 的预乘 Alpha 约定。
    /// 调用前需提交待绘制的 SpriteBatch。整像素偏移可复用既有的边缘覆盖率。
    /// scissor 为真时使用设备现有的 ScissorRectangle。可传入绘制后恢复的 EffectPass。
    /// </summary>
    /// <param name="mesh">与当前视口像素坐标相符的预制网格。</param>
    /// <param name="color">预乘 Alpha 的绘制颜色。</param>
    /// <param name="offset">以设备像素为单位的额外平移。</param>
    /// <param name="scissor">是否启用设备当前的矩形裁剪。</param>
    /// <param name="resumePass">绘制完成后要重新应用的 Effect pass，可为 <see langword="null"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="mesh"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ObjectDisposedException">绘制器已释放。</exception>
    public void Draw(PathMesh mesh, Color color, Point offset = default, bool scissor = false, EffectPass resumePass = null) =>
        DrawCore(mesh, color, null, offset, scissor, resumePass);

    /// <summary>向当前 FNA 渲染目标立即绘制带渐变的预制网格。调用前需提交待绘制的批次。</summary>
    /// <param name="mesh">与当前视口像素坐标相符的预制网格。</param>
    /// <param name="gradient">准备网格前的路径局部坐标中的渐变画笔。</param>
    /// <param name="offset">以设备像素为单位，同时平移网格和渐变。</param>
    /// <param name="scissor">是否启用当前矩形裁剪。</param>
    /// <param name="resumePass">绘制完成后要重新应用的 Effect pass。</param>
    public void DrawGradient(PathMesh mesh, PathGradient gradient, Point offset = default, bool scissor = false, EffectPass resumePass = null)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        DrawCore(mesh, Color.White, gradient, offset, scissor, resumePass);
    }

    private void DrawCore(PathMesh mesh, Color color, PathGradient gradient, Point offset, bool scissor, EffectPass resumePass)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(mesh);
        var viewport = _device.Viewport;
        if (mesh.IsEmpty || color.A == 0 || viewport.Width <= 0 || viewport.Height <= 0) return;

        var blend = _device.BlendState;

        var depth = _device.DepthStencilState;

        var rasterizer = _device.RasterizerState;

        var texture = _device.Textures[0];

        var sampler = _device.SamplerStates[0];
        try
        {
            _device.BlendState = BlendState.AlphaBlend;
            _device.DepthStencilState = DepthStencilState.None;
            _device.RasterizerState = scissor ? SilkyUI.ScissorRasterizerState : RasterizerState.CullNone;
            var translation = Matrix.CreateTranslation(offset.X, offset.Y, 0f);

            var projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            if (gradient is null)
            {
                _effect.World = translation;
                _effect.Projection = projection;
                _effect.Alpha = color.A / 255f;
                // BasicEffect 会根据 Alpha 预乘漫反射颜色，此处先还原调用方传入的 RGB。
                _effect.DiffuseColor = new Vector3(color.R, color.G, color.B) / color.A;
                _effect.CurrentTechnique.Passes[0].Apply();
            }
            else
            {
                if (!GetGradientCoordinates(gradient, mesh.LocalToPixel, out var x, out var y)) return;
                ApplyGradient(gradient, translation * projection, x, y);
            }
            _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                mesh.VertexBuffer, 0, mesh.Vertices.Length, mesh.IndexBuffer, 0, mesh.Indices.Length / 3);
        }
        finally
        {
            try
            {
                resumePass?.Apply();
            }
            finally
            {
                _device.BlendState = blend;
                _device.DepthStencilState = depth;
                _device.RasterizerState = rasterizer;
                _device.Textures[0] = texture;
                _device.SamplerStates[0] = sampler;
            }
        }
    }

    private void ApplyGradient(PathGradient gradient, Matrix projection, Vector3 x, Vector3 y)
    {
        var effect = ModAsset.PathGradientEffect.Value;
        if (effect.GraphicsDevice != _device)
            throw new InvalidOperationException("渐变绘制器必须使用加载 SilkyUI 着色器的图形设备。");
        var parameters = effect.Parameters;
        parameters["uProjection"].SetValue(projection);
        parameters["uGradientX"].SetValue(x);
        parameters["uGradientY"].SetValue(y);
        parameters["uRadial"].SetValue(gradient.IsRadial);
        parameters["uTileMode"].SetValue((float)gradient.TileMode);
        parameters["uPremultiplied"].SetValue(gradient.Premultiplied ? 1f : 0f);
        parameters["uLinearRgb"].SetValue(gradient.LinearRgb ? 1f : 0f);
        var stops = gradient.Stops;
        if (stops.Length == 1 || (stops.Length == 2 && stops[0].Position == 0f && stops[1].Position == 1f))
        {
            parameters["uFirstColor"].SetValue(stops[0].Color);
            parameters["uLastColor"].SetValue(stops[^1].Color);
            effect.CurrentTechnique.Passes["SimpleGradient"].Apply();
            return;
        }

        var texture = _gradientStops.GetValue(gradient, CreateStopTexture);
        parameters["uStopCount"].SetValue((float)texture.Width);
        parameters["uInverseStopCount"].SetValue(1f / texture.Width);
        effect.CurrentTechnique.Passes["Gradient"].Apply();
        _device.Textures[0] = texture;
        _device.SamplerStates[0] = SamplerState.PointClamp;
    }

    private Texture2D CreateStopTexture(PathGradient gradient)
    {
        var count = gradient.Stops.Length;

        var data = new Vector4[count * 2];
        for (int i = 0; i < count; i++)
        {
            data[i] = gradient.Stops[i].Color;
            data[count + i].X = gradient.Stops[i].Position;
        }
        var texture = new Texture2D(_device, count, 2, false, SurfaceFormat.Vector4);
        try
        {
            texture.SetData(data);
            return texture;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }

    private static bool GetGradientCoordinates(PathGradient gradient, Matrix localToPixel, out Vector3 x, out Vector3 y)
    {
        x = y = Vector3.Zero;
        if (gradient.Stops.Length == 1) return true;

        // 按 Skia 的顺序撤销路径与渐变局部变换，再映射到单位渐变坐标。
        var local = gradient.LocalMatrix;

        var a = (double)local.M11 * localToPixel.M11 + (double)local.M12 * localToPixel.M21;

        var b = (double)local.M11 * localToPixel.M12 + (double)local.M12 * localToPixel.M22;

        var c = (double)local.M21 * localToPixel.M11 + (double)local.M22 * localToPixel.M21;

        var d = (double)local.M21 * localToPixel.M12 + (double)local.M22 * localToPixel.M22;

        var determinant = a * d - b * c;
        if (determinant == 0d) return false;
        var originX = gradient.Origin.X * a + gradient.Origin.Y * c
            + (double)local.M41 * localToPixel.M11 + (double)local.M42 * localToPixel.M21 + localToPixel.M41;

        var originY = gradient.Origin.X * b + gradient.Origin.Y * d
            + (double)local.M41 * localToPixel.M12 + (double)local.M42 * localToPixel.M22 + localToPixel.M42;
        x = Row((d * gradient.X - b * gradient.Y) / determinant, (a * gradient.Y - c * gradient.X) / determinant);
        if (gradient.IsRadial)
            y = Row(-b * gradient.X / determinant, a * gradient.X / determinant);
        return IsFinite(x) && IsFinite(y);

        Vector3 Row(double dx, double dy) => new((float)dx, (float)dy, (float)(-originX * dx - originY * dy));

        static bool IsFinite(Vector3 row) => float.IsFinite(row.X) && float.IsFinite(row.Y) && float.IsFinite(row.Z);
    }

    private Rectangle GetUiPixelBounds()
    {
        var viewport = _device.Viewport;

        var clip = _device.ScissorRectangle;
        clip.Offset(-viewport.X, -viewport.Y);
        return Rectangle.Intersect(new Rectangle(0, 0, viewport.Width, viewport.Height), clip);
    }

    private Matrix GetUiTransform(SpriteBatch spriteBatch)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(spriteBatch);
        if (spriteBatch.GraphicsDevice != _device)
            throw new ArgumentException("绘制批次与路径绘制器必须使用同一图形设备。", nameof(spriteBatch));
        return spriteBatch.transformMatrix;
    }

    /// <summary>在图形线程释放绘制器持有的 Effect 与渐变查表纹理。可重复调用。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _effect.Dispose();
        foreach (var pair in _gradientStops) pair.Value.Dispose();
        _gradientStops.Clear();
    }
}
