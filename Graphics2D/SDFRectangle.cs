namespace SilkyUIFramework.Graphics2D;

/// <summary>
/// SDF 矩形绘制门面：负责参数绑定、pass 选择与图元提交。
/// 几何顶点构建由 <see cref="SDFRectangleGeometryBuilder"/> 负责。
/// </summary>
public static class SDFRectangle
{
    /// <summary>
    /// SpriteBatch 默认特效 pass，用于在自定义绘制后恢复主批次状态。
    /// </summary>
    public static EffectPass SpriteEffectPass => Main.spriteBatch.spriteEffectPass;
    /// <summary>
    /// 当前图形设备。
    /// </summary>
    public static GraphicsDevice GraphicsDevice => Main.graphics.GraphicsDevice;
    /// <summary>
    /// SDF 矩形着色器实例。
    /// </summary>
    private static Effect Effect => ModAsset.SDFRectangle.Value;

    /// <summary>
    /// 绘制带描边的圆角矩形。
    /// 内部会根据当前缩放计算抗锯齿过渡范围，并使用 HasBorder pass。
    /// </summary>
    public static void DrawWithBorder(Vector2 position, Vector2 size,
        Vector4 borderRadius, Color backgroundColor, float border, Color borderColor, Matrix matrix)
    {
        var edgePadding = 1 / matrix.M11;
        matrix = PrepareSdfMatrix(matrix);

        var effect = Effect;

        effect.Parameters["uBorder"].SetValue(border);
        effect.Parameters["uBorderColor"].SetValue(borderColor.ToVector4());

        effect.Parameters["uTransformMatrix"].SetValue(matrix);
        effect.Parameters["uBackgroundColor"].SetValue(backgroundColor.ToVector4());
        effect.CurrentTechnique.Passes["HasBorder"].Apply();

        SetRectanglePrimitives(edgePadding, position, size, borderRadius);

        SubmitRectanglePrimitives();
    }

    /// <summary>
    /// 绘制无描边的圆角矩形（NoBorder pass）。
    /// </summary>
    public static void DrawWithoutBorder(Vector2 position, Vector2 size,
        Vector4 borderRadius, Color backgroundColor, Matrix matrix)
    {
        var edgePadding = 1 / matrix.M11;
        matrix = PrepareSdfMatrix(matrix);

        var effect = Effect;

        effect.Parameters["uTransformMatrix"].SetValue(matrix);
        effect.Parameters["uBackgroundColor"].SetValue(backgroundColor.ToVector4());
        effect.CurrentTechnique.Passes["NoBorder"].Apply();

        SetRectanglePrimitives(edgePadding, position, size, borderRadius);

        SubmitRectanglePrimitives();
    }

    /// <summary>
    /// 采样整张纹理并按圆角矩形裁剪后绘制。
    /// 纹理坐标根据当前 Viewport 自动推导。
    /// </summary>
    public static void SampleVersion(Texture2D texture2D, Vector2 position, Vector2 size, Vector4 borderRadius, Matrix matrix)
    {
        var edgePadding = 1 / matrix.M11;
        matrix = PrepareSdfMatrix(matrix);
        var device = GraphicsDevice;
        var screenSize = new Vector2(device.Viewport.Width, device.Viewport.Height);

        var effect = Effect;

        effect.Parameters["uTransformMatrix"].SetValue(matrix);
        effect.Parameters["uBackgroundColor"].SetValue(Color.White.ToVector4());
        effect.CurrentTechnique.Passes["SampleVersion"].Apply();

        device.Textures[0] = texture2D;
        SetRectanglePrimitives(edgePadding, position, size, borderRadius, position / screenSize, size / screenSize);

        SubmitRectanglePrimitives();
    }

    /// <summary>
    /// 采样纹理指定 UV 区域并按圆角矩形裁剪后绘制。
    /// </summary>
    public static void SampleVersion(Texture2D texture2D, Vector2 position, Vector2 size,
        Vector2 textureCoordinatesPosition, Vector2 textureCoordinatesSize, Vector4 borderRadius, Color color, Matrix matrix)
    {
        matrix = PrepareSdfMatrix(matrix);
        var device = GraphicsDevice;

        var effect = Effect;

        effect.Parameters["uTransformMatrix"].SetValue(matrix);
        effect.Parameters["uBackgroundColor"].SetValue(color.ToVector4());
        effect.CurrentTechnique.Passes["SampleVersion"].Apply();

        device.Textures[0] = texture2D;
        SetRectanglePrimitives(0, position, size, borderRadius, textureCoordinatesPosition, textureCoordinatesSize);

        SubmitRectanglePrimitives();
    }

    /// <summary>
    /// 绘制圆角矩形阴影（Shadow pass）。
    /// </summary>
    public static void DrawShadow(Vector2 position, Vector2 size,
        Vector4 borderRadius, Color backgroundColor, float shadowBlurSize, Matrix matrix)
    {
        matrix = PrepareSdfMatrix(matrix);

        var effect = Effect;

        effect.Parameters["uTransformMatrix"].SetValue(matrix);
        effect.Parameters["uBackgroundColor"].SetValue(backgroundColor.ToVector4());
        effect.Parameters["uShadowBlurSize"].SetValue(shadowBlurSize);
        effect.CurrentTechnique.Passes["Shadow"].Apply();
        SetRectanglePrimitives(0f, position, size, borderRadius);

        SubmitRectanglePrimitives();
    }

    /// <summary>
    /// 将 SpriteBatch 变换矩阵转换到 SDF 所需坐标空间，并设置边缘抗锯齿区间。
    /// smoothstep 计算依赖缩放分量 <c>M11</c>，调用方需保证其非 0。
    /// </summary>
    private static Matrix PrepareSdfMatrix(Matrix matrix)
    {
        const float root2Over2 = 1.414213562373f / 2f;
        var zoom = matrix.M11;
        Effect.Parameters["uSmoothstepRange"].SetValue(new Vector2(-root2Over2 / zoom, root2Over2 / zoom));

        return MatrixHelper.Transform2SDFMatrix(matrix);
    }

    /// <summary>
    /// 提交已写入的矩形图元，并恢复 SpriteBatch 默认 pass。
    /// 调用前应已完成 effect 参数设置与 pass.Apply。
    /// </summary>
    private static void SubmitRectanglePrimitives()
    {
        GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
            SDFRectangleGeometryBuilder.RectangleVertexData, 0, SDFRectangleGeometryBuilder.RectangleVertexData.Length,
            SDFRectangleGeometryBuilder.IndexData, 0, 8);

        SpriteEffectPass.Apply();
    }

    /// <summary>
    /// 纯色矩形顶点构建委托到几何构建器。
    /// </summary>
    private static void SetRectanglePrimitives(float edgePadding, Vector2 position, Vector2 size, Vector4 borderRadius)
        => SDFRectangleGeometryBuilder.SetRectanglePrimitives(edgePadding, position, size, borderRadius);

    /// <summary>
    /// 纹理矩形顶点构建委托到几何构建器。
    /// </summary>
    private static void SetRectanglePrimitives(float edgePadding, Vector2 position, Vector2 size, Vector4 borderRadius,
        Vector2 textureCoordinatesPosition, Vector2 textureCoordinatesSize)
        => SDFRectangleGeometryBuilder.SetRectanglePrimitives(edgePadding, position, size, borderRadius, textureCoordinatesPosition, textureCoordinatesSize);
}
