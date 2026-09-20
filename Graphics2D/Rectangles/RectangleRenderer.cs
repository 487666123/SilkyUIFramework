using System;

namespace SilkyUIFramework.Graphics2D.Rectangles;

/// <summary>
/// 立即绘制圆角矩形，每个实例复用自己的顶点与索引数组。
/// 使用 RectangleEffect shader，支持四边独立宽度与颜色。
/// </summary>
/// <remarks>
/// 在图形线程串行调用。调用方负责 SpriteBatch 提交顺序、混合、采样、裁剪及光栅化状态。
/// 颜色和纹理遵循现有 shader 的预乘 Alpha 约定；transform 使用正的等比缩放和平移。
/// 所有 cornerRadii 参数的分量顺序均为左上、右上、左下、右下。
/// </remarks>
public sealed class RectangleRenderer
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly EffectPass resumePass;
    private readonly SDFGraphicsVertexType[] vertices = new SDFGraphicsVertexType[RectangleGeometryBuilder.VertexCount];
    private readonly short[] indices = new short[RectangleGeometryBuilder.IndexCount];

    private readonly EffectParameter transformParameter;
    private readonly EffectParameter antialiasRangeParameter;
    private readonly EffectParameter colorParameter;
    private readonly EffectParameter borderWidthParameter;
    private readonly EffectParameter borderColorParameter;
    private readonly EffectParameter shadowBlurParameter;

    private readonly EffectParameter innerOriginParameter;
    private readonly EffectParameter innerSizeParameter;
    private readonly EffectParameter innerInverseRadiusXParameter;
    private readonly EffectParameter innerInverseRadiusYParameter;
    private readonly EffectParameter innerCornerMaskParameter;

    private readonly EffectParameter rectangleOriginParameter;
    private readonly EffectParameter rectangleSizeParameter;
    private readonly EffectParameter borderColorLeftParameter;
    private readonly EffectParameter borderColorTopParameter;
    private readonly EffectParameter borderColorRightParameter;
    private readonly EffectParameter borderColorBottomParameter;
    private readonly EffectParameter borderTopCornerWeightsParameter;
    private readonly EffectParameter borderBottomCornerWeightsParameter;
    private readonly EffectParameter borderOppositeSplitsParameter;
    private readonly EffectParameter borderSideMaskParameter;
    private readonly EffectParameter innerEnabledParameter;

    private readonly EffectPass fillPass;
    private readonly EffectPass uniformBorderPass;
    private readonly EffectPass texturedPass;
    private readonly EffectPass shadowPass;
    private readonly EffectPass perSideBorderPass;
    private readonly EffectPass perSideBorderColorsPass;

    /// <summary>
    /// 借用设备、RectangleEffect 和绘制结束后需要恢复的 pass，不接管它们的释放。
    /// Effect 必须来自同一设备，使用 <c>ModAsset.RectangleEffect.Value</c>。
    /// 构造时要求全部参数及 Fill、UniformBorder、Textured、Shadow、
    /// PerSideBorder、PerSideBorderColors 六个 pass。
    /// 应复用绘制器实例；资源重新加载后，应使用新的 Effect 和恢复 pass 重新创建实例。
    /// </summary>
    public RectangleRenderer(GraphicsDevice graphicsDevice, Effect effect, EffectPass resumePass)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(resumePass);

        this.graphicsDevice = graphicsDevice;
        this.resumePass = resumePass;

        // 集中绑定 shader 参数与 pass，绘制时复用缓存引用。
        transformParameter = RequireParameter(effect, "uTransformMatrix");
        antialiasRangeParameter = RequireParameter(effect, "uAntialiasRange");
        colorParameter = RequireParameter(effect, "uColor");
        borderWidthParameter = RequireParameter(effect, "uBorderWidth");
        borderColorParameter = RequireParameter(effect, "uBorderColor");
        shadowBlurParameter = RequireParameter(effect, "uShadowBlurSize");

        fillPass = RequirePass(effect, "Fill");
        uniformBorderPass = RequirePass(effect, "UniformBorder");
        texturedPass = RequirePass(effect, "Textured");
        shadowPass = RequirePass(effect, "Shadow");

        perSideBorderPass = RequirePass(effect, "PerSideBorder");
        perSideBorderColorsPass = RequirePass(effect, "PerSideBorderColors");

        innerOriginParameter = RequireParameter(effect, "uInnerOrigin");
        innerSizeParameter = RequireParameter(effect, "uInnerSize");
        innerInverseRadiusXParameter = RequireParameter(effect, "uInnerInvRadiusX");
        innerInverseRadiusYParameter = RequireParameter(effect, "uInnerInvRadiusY");
        innerCornerMaskParameter = RequireParameter(effect, "uInnerCornerMask");

        rectangleOriginParameter = RequireParameter(effect, "uRectangleOrigin");
        rectangleSizeParameter = RequireParameter(effect, "uRectangleSize");
        borderColorLeftParameter = RequireParameter(effect, "uBorderColorLeft");
        borderColorTopParameter = RequireParameter(effect, "uBorderColorTop");
        borderColorRightParameter = RequireParameter(effect, "uBorderColorRight");
        borderColorBottomParameter = RequireParameter(effect, "uBorderColorBottom");
        borderTopCornerWeightsParameter = RequireParameter(effect, "uBorderTopCornerWeights");
        borderBottomCornerWeightsParameter = RequireParameter(effect, "uBorderBottomCornerWeights");
        borderOppositeSplitsParameter = RequireParameter(effect, "uBorderOppositeSplits");
        borderSideMaskParameter = RequireParameter(effect, "uBorderSideMask");
        innerEnabledParameter = RequireParameter(effect, "uInnerEnabled");

        RectangleGeometryBuilder.WriteIndices(indices);
    }

    /// <summary>绘制纯色填充的圆角矩形。</summary>
    public void DrawFill(Vector2 position, Vector2 size, Vector4 cornerRadii, Color fillColor, Matrix transform)
    {
        BindTransform(transform);
        colorParameter.SetValue(fillColor.ToVector4());
        RectangleGeometryBuilder.WriteVertices(vertices, position, size, cornerRadii, 1f / transform.M11);
        Submit(fillPass);
    }

    /// <summary>绘制内部填充及向内延伸的描边，borderWidth 使用局部坐标单位。</summary>
    public void DrawBordered(Vector2 position, Vector2 size, Vector4 cornerRadii,
        Color fillColor, float borderWidth, Color borderColor, Matrix transform)
    {
        BindTransform(transform);
        colorParameter.SetValue(fillColor.ToVector4());
        borderWidthParameter.SetValue(borderWidth);
        borderColorParameter.SetValue(borderColor.ToVector4());
        RectangleGeometryBuilder.WriteVertices(vertices, position, size, cornerRadii, 1f / transform.M11);
        Submit(uniformBorderPass);
    }

    /// <summary>
    /// 绘制四边宽度独立、颜色相同的内描边。borderWidths 的顺序为左、上、右、下。
    /// 负边宽和负圆角按零处理；等宽和不等宽都使用独立内轮廓。
    /// 此重载统一用内外覆盖率之差绘制描边，避免宽度变动时切换混色公式。
    /// </summary>
    public void DrawBordered(Vector2 position, Vector2 size, Vector4 cornerRadii,
        Color fillColor, Vector4 borderWidths, Color borderColor, Matrix transform)
    {
        if (size.X <= 0f || size.Y <= 0f)
            return;

        borderWidths = Vector4.Max(borderWidths, Vector4.Zero);
        cornerRadii = Vector4.Max(cornerRadii, Vector4.Zero);

        if (borderWidths == Vector4.Zero)
        {
            DrawFill(position, size, cornerRadii, fillColor, transform);
            return;
        }

        var edgePadding = 1f / transform.M11;
        var inner = RectangleGeometryBuilder.BuildInnerContour(
            position, size, cornerRadii, borderWidths, edgePadding);

        // 边宽吃完内部区域时，整个外轮廓都由边框色填充。
        if (!inner.HasArea)
        {
            DrawFill(position, size, cornerRadii, borderColor, transform);
            return;
        }

        BindTransform(transform);
        colorParameter.SetValue(fillColor.ToVector4());
        borderColorParameter.SetValue(borderColor.ToVector4());
        BindInnerContour(inner);

        RectangleGeometryBuilder.WriteVertices(vertices, position, size, cornerRadii, edgePadding);
        Submit(perSideBorderPass);
    }

    /// <summary>
    /// 绘制四边宽度和颜色都独立的内描边，边宽及颜色参数顺序均为左、上、右、下。
    /// 颜色按到各边的距离与边宽之比分区，交界处抗锯齿；零宽边不参与颜色混合。
    /// </summary>
    /// <remarks>颜色遵循预乘 Alpha 约定。四种颜色相同时复用单色四边重载。</remarks>
    public void DrawBordered(Vector2 position, Vector2 size, Vector4 cornerRadii,
        Color fillColor, Vector4 borderWidths,
        Color leftBorderColor, Color topBorderColor, Color rightBorderColor, Color bottomBorderColor,
        Matrix transform)
    {
        if (size.X <= 0f || size.Y <= 0f)
            return;

        borderWidths = Vector4.Max(borderWidths, Vector4.Zero);
        cornerRadii = Vector4.Max(cornerRadii, Vector4.Zero);

        if (borderWidths == Vector4.Zero)
        {
            DrawFill(position, size, cornerRadii, fillColor, transform);
            return;
        }

        if (leftBorderColor == topBorderColor && leftBorderColor == rightBorderColor &&
            leftBorderColor == bottomBorderColor)
        {
            DrawBordered(position, size, cornerRadii, fillColor, borderWidths, leftBorderColor, transform);
            return;
        }

        var edgePadding = 1f / transform.M11;
        var inner = RectangleGeometryBuilder.BuildInnerContour(
            position, size, cornerRadii, borderWidths, edgePadding);
        var partition = RectangleGeometryBuilder.BuildBorderColorPartition(size, borderWidths);

        BindTransform(transform);
        colorParameter.SetValue(fillColor.ToVector4());
        BindInnerContour(inner);
        rectangleOriginParameter.SetValue(position);
        rectangleSizeParameter.SetValue(size);
        borderColorLeftParameter.SetValue(leftBorderColor.ToVector4());
        borderColorTopParameter.SetValue(topBorderColor.ToVector4());
        borderColorRightParameter.SetValue(rightBorderColor.ToVector4());
        borderColorBottomParameter.SetValue(bottomBorderColor.ToVector4());
        borderTopCornerWeightsParameter.SetValue(partition.TopCornerWeights);
        borderBottomCornerWeightsParameter.SetValue(partition.BottomCornerWeights);
        borderOppositeSplitsParameter.SetValue(partition.OppositeSplits);
        borderSideMaskParameter.SetValue(partition.EnabledSides);
        // 内部区域消失时仍要保留颜色分区，不能退回单色 DrawFill。
        innerEnabledParameter.SetValue(inner.HasArea ? 1f : 0f);

        RectangleGeometryBuilder.WriteVertices(vertices, position, size, cornerRadii, edgePadding);
        Submit(perSideBorderColorsPass);
    }

    private void BindInnerContour(RectangleGeometryBuilder.InnerContour inner)
    {
        innerOriginParameter.SetValue(inner.Origin);
        innerSizeParameter.SetValue(inner.Size);
        innerInverseRadiusXParameter.SetValue(inner.InverseRadiusX);
        innerInverseRadiusYParameter.SetValue(inner.InverseRadiusY);
        innerCornerMaskParameter.SetValue(inner.RoundedCorners);
    }

    /// <summary>
    /// 采样指定 UV 区域，并按圆角矩形裁剪。uvOrigin 为左上角，uvSize 为归一化坐标尺寸。
    /// 沿用原显式 UV 入口的零外扩行为；传入 Vector2.Zero 和 Vector2.One 可采样整张纹理。
    /// </summary>
    public void DrawTexture(Texture2D texture, Vector2 position, Vector2 size,
        Vector2 uvOrigin, Vector2 uvSize, Vector4 cornerRadii, Color tintColor, Matrix transform)
    {
        DrawTextured(texture, position, size, uvOrigin, uvSize, cornerRadii, tintColor, transform, 0f);
    }

    /// <summary>
    /// 以 position / BackBufferSize 和 size / BackBufferSize 推导 UV，采样屏幕对应的纹理区域。
    /// position 和 size 必须采用对应的 BackBuffer 坐标；UV 不应用 transform。
    /// </summary>
    public void DrawScreenTexture(Texture2D texture, Vector2 position, Vector2 size,
        Vector4 cornerRadii, Matrix transform)
    {
        var presentation = graphicsDevice.PresentationParameters;
        var screenSize = new Vector2(presentation.BackBufferWidth, presentation.BackBufferHeight);
        DrawTextured(texture, position, size, position / screenSize, size / screenSize,
            cornerRadii, Color.White, transform, 1f / transform.M11);
    }

    /// <summary>
    /// 在给定矩形范围内绘制向内部淡出的阴影，blurSize 使用局部坐标单位。
    /// 阴影偏移、外扩尺寸和相应圆角由调用方计算，此方法不会自动向外扩展阴影。
    /// </summary>
    public void DrawShadow(Vector2 position, Vector2 size, Vector4 cornerRadii,
        Color shadowColor, float blurSize, Matrix transform)
    {
        BindTransform(transform);
        colorParameter.SetValue(shadowColor.ToVector4());
        shadowBlurParameter.SetValue(blurSize);
        RectangleGeometryBuilder.WriteVertices(vertices, position, size, cornerRadii, 0f);
        Submit(shadowPass);
    }

    private void DrawTextured(Texture2D texture, Vector2 position, Vector2 size,
        Vector2 uvOrigin, Vector2 uvSize, Vector4 cornerRadii, Color tintColor, Matrix transform, float edgePadding)
    {
        ArgumentNullException.ThrowIfNull(texture);
        BindTransform(transform);
        colorParameter.SetValue(tintColor.ToVector4());
        RectangleGeometryBuilder.WriteVertices(vertices, position, size, cornerRadii, edgePadding);
        RectangleGeometryBuilder.WriteTextureCoordinates(vertices, uvOrigin, uvSize);
        Submit(texturedPass, texture);
    }

    /// <summary>统一绑定投影矩阵与抗锯齿范围，不改变调用方的矩阵。</summary>
    private void BindTransform(Matrix transform)
    {
        const float halfDiagonal = 1.414213562373f / 2f;
        var antialiasHalfWidth = halfDiagonal / transform.M11;
        antialiasRangeParameter.SetValue(new Vector2(-antialiasHalfWidth, antialiasHalfWidth));

        // 与现有 MatrixHelper.Transform2SDFMatrix 保持相同投影约定，使用实例设备的 Viewport。
        var viewport = graphicsDevice.Viewport;
        transform *= Matrix.CreateScale(2f / viewport.Width, -2f / viewport.Height, 1f);
        transform *= Matrix.CreateTranslation(
            -1f + transform.M41 * 2f / viewport.Width,
            1f - transform.M42 * 2f / viewport.Height, 0f);
        transformParameter.SetValue(transform);
    }

    /// <summary>立即提交，并恢复指定 pass 及纹理槽 0；不接管其他设备状态。</summary>
    private void Submit(EffectPass pass, Texture2D texture = null)
    {
        var previousTexture = graphicsDevice.Textures[0];
        try
        {
            pass.Apply();
            if (texture is not null)
                graphicsDevice.Textures[0] = texture;

            graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList,
                vertices, 0, vertices.Length, indices, 0, RectangleGeometryBuilder.PrimitiveCount);
        }
        finally
        {
            resumePass.Apply();
            graphicsDevice.Textures[0] = previousTexture;
        }
    }

    private static EffectParameter RequireParameter(Effect effect, string name)
        => effect.Parameters[name]
            ?? throw new ArgumentException($"矩形 Effect 缺少参数：{name}", nameof(effect));

    private static EffectPass RequirePass(Effect effect, string name)
        => effect.CurrentTechnique.Passes[name]
            ?? throw new ArgumentException($"矩形 Effect 的当前 technique 缺少 pass：{name}", nameof(effect));
}
