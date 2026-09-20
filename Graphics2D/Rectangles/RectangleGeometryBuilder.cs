using System;

namespace SilkyUIFramework.Graphics2D.Rectangles;

/// <summary>
/// 将圆角矩形分成四个象限，向调用方提供的数组写入数据，不保存绘制状态。
/// </summary>
internal static class RectangleGeometryBuilder
{
    private const int QuadrantCount = 4;
    private const int VerticesPerQuadrant = 4;
    private const int IndicesPerQuadrant = 6;

    internal const int VertexCount = QuadrantCount * VerticesPerQuadrant;
    internal const int IndexCount = QuadrantCount * IndicesPerQuadrant;
    internal const int PrimitiveCount = IndexCount / 3;

    /// <summary>索引只需在绘制器初始化时填写一次。</summary>
    internal static void WriteIndices(Span<short> destination)
    {
        for (var quadrant = 0; quadrant < QuadrantCount; quadrant++)
        {
            var vertexOffset = quadrant * VerticesPerQuadrant;
            var indexOffset = quadrant * IndicesPerQuadrant;
            destination[indexOffset] = (short)vertexOffset;
            destination[indexOffset + 1] = (short)(vertexOffset + 1);
            destination[indexOffset + 2] = (short)(vertexOffset + 2);
            destination[indexOffset + 3] = (short)(vertexOffset + 2);
            destination[indexOffset + 4] = (short)(vertexOffset + 1);
            destination[indexOffset + 5] = (short)(vertexOffset + 3);
        }
    }

    /// <summary>
    /// 写入位置、SDF 坐标及圆角半径，并将 UV 初始化为零。
    /// cornerRadii 的顺序为左上、右上、左下、右下；尺寸和 padding 均使用局部坐标单位。
    /// </summary>
    internal static void WriteVertices(Span<SDFGraphicsVertexType> destination,
        Vector2 position, Vector2 size, Vector4 cornerRadii, float edgePadding)
    {
        var paddedPosition = position - new Vector2(edgePadding);
        var halfSize = (size + new Vector2(edgePadding * 2f)) * 0.5f;

        for (var quadrant = 0; quadrant < QuadrantCount; quadrant++)
        {
            // 象限和象限内的顶点都按左上、右上、左下、右下排列。
            var cornerX = quadrant & 1;
            var cornerY = quadrant >> 1;
            var quadrantPosition = paddedPosition + new Vector2(cornerX * halfSize.X, cornerY * halfSize.Y);
            var cornerRadius = quadrant switch
            {
                0 => cornerRadii.X,
                1 => cornerRadii.Y,
                2 => cornerRadii.Z,
                _ => cornerRadii.W
            };

            // 保留现有 shader 的几何约定：半径增加 padding，SDF 坐标再偏移 padding。
            var paddedRadius = cornerRadius + edgePadding;
            var sdfOrigin = paddedRadius + edgePadding;

            for (var vertex = 0; vertex < VerticesPerQuadrant; vertex++)
            {
                var localX = vertex & 1;
                var localY = vertex >> 1;
                var vertexPosition = quadrantPosition + new Vector2(localX * halfSize.X, localY * halfSize.Y);

                // 把各象限的外角折叠到同一方向，供 shader 的距离公式使用。
                var sdfCoordinates = new Vector2(
                    sdfOrigin - (localX == cornerX ? 0f : halfSize.X),
                    sdfOrigin - (localY == cornerY ? 0f : halfSize.Y));

                destination[quadrant * VerticesPerQuadrant + vertex] = new SDFGraphicsVertexType(
                    vertexPosition, Vector2.Zero, sdfCoordinates, paddedRadius);
            }
        }
    }

    /// <summary>
    /// 根据左、上、右、下边宽推导内轮廓，半径计算沿用现有外轮廓的 padding 约定。
    /// </summary>
    internal static InnerContour BuildInnerContour(Vector2 position, Vector2 size,
        Vector4 cornerRadii, Vector4 borderWidths, float edgePadding)
    {
        var origin = position + new Vector2(borderWidths.X, borderWidths.Y);
        var innerSize = size - new Vector2(
            borderWidths.X + borderWidths.Z, borderWidths.Y + borderWidths.W);

        if (innerSize.X <= 0f || innerSize.Y <= 0f)
            return new InnerContour(origin, Vector2.Zero, Vector4.Zero, Vector4.Zero, Vector4.Zero);

        // WriteVertices 实际传给 shader 的半径比调用方半径多一个 padding。
        var outerRadii = cornerRadii + new Vector4(edgePadding);
        var topLeft = GetInnerRadii(outerRadii.X, borderWidths.X, borderWidths.Y);
        var topRight = GetInnerRadii(outerRadii.Y, borderWidths.Z, borderWidths.Y);
        var bottomLeft = GetInnerRadii(outerRadii.Z, borderWidths.X, borderWidths.W);
        var bottomRight = GetInnerRadii(outerRadii.W, borderWidths.Z, borderWidths.W);

        // 在内矩形很窄时，等比缩小全部内圆角，避免相邻圆角超过可用边长。
        var maxHorizontal = MathF.Max(topLeft.X + topRight.X, bottomLeft.X + bottomRight.X);
        var maxVertical = MathF.Max(topLeft.Y + bottomLeft.Y, topRight.Y + bottomRight.Y);
        var scale = 1f;
        if (maxHorizontal > 0f)
            scale = MathF.Min(scale, innerSize.X / maxHorizontal);
        if (maxVertical > 0f)
            scale = MathF.Min(scale, innerSize.Y / maxVertical);

        var tl = EncodeInnerCorner(topLeft * scale);
        var tr = EncodeInnerCorner(topRight * scale);
        var bl = EncodeInnerCorner(bottomLeft * scale);
        var br = EncodeInnerCorner(bottomRight * scale);

        return new InnerContour(origin, innerSize,
            new Vector4(tl.X, tr.X, bl.X, br.X),
            new Vector4(tl.Y, tr.Y, bl.Y, br.Y),
            new Vector4(tl.Z, tr.Z, bl.Z, br.Z));
    }

    private static Vector2 GetInnerRadii(float outerRadius, float horizontalWidth, float verticalWidth)
    {
        var radiusX = outerRadius - horizontalWidth;
        var radiusY = outerRadius - verticalWidth;
        // 任一轴退化为零时，该内角按直角处理。
        return radiusX > 0f && radiusY > 0f ? new Vector2(radiusX, radiusY) : Vector2.Zero;
    }

    private static Vector3 EncodeInnerCorner(Vector2 radii)
        => radii.X > 0f && radii.Y > 0f
            ? new Vector3(1f / radii.X, 1f / radii.Y, 1f)
            : Vector3.Zero;

    // 值类型结果不拥有缓存，也不产生每次绘制的数组分配。
    internal readonly record struct InnerContour(
        Vector2 Origin, Vector2 Size, Vector4 InverseRadiusX, Vector4 InverseRadiusY, Vector4 RoundedCorners)
    {
        internal bool HasArea => Size.X > 0f && Size.Y > 0f;
    }

    /// <summary>
    /// 预计算四边颜色的分界线。边宽顺序为左、上、右、下；零宽边不参与分区。
    /// 所有系数使用局部长度单位，shader 可直接复用轮廓的抗锯齿区间。
    /// </summary>
    internal static BorderColorPartition BuildBorderColorPartition(Vector2 size, Vector4 borderWidths)
    {
        var topLeft = NormalizeWidths(borderWidths.X, borderWidths.Y);
        var topRight = NormalizeWidths(borderWidths.Z, borderWidths.Y);
        var bottomLeft = NormalizeWidths(borderWidths.X, borderWidths.W);
        var bottomRight = NormalizeWidths(borderWidths.Z, borderWidths.W);

        return new BorderColorPartition(
            new Vector4(topLeft.X, topLeft.Y, topRight.X, topRight.Y),
            new Vector4(bottomLeft.X, bottomLeft.Y, bottomRight.X, bottomRight.Y),
            new Vector2(
                GetOppositeSplit(size.X, borderWidths.X, borderWidths.Z),
                GetOppositeSplit(size.Y, borderWidths.Y, borderWidths.W)),
            new Vector4(
                borderWidths.X > 0f ? 1f : 0f,
                borderWidths.Y > 0f ? 1f : 0f,
                borderWidths.Z > 0f ? 1f : 0f,
                borderWidths.W > 0f ? 1f : 0f));
    }

    private static Vector2 NormalizeWidths(float horizontalWidth, float verticalWidth)
    {
        var largest = MathF.Max(horizontalWidth, verticalWidth);
        if (largest <= 0f)
            return Vector2.Zero;

        // 先缩放再求长度，避免大边宽在平方时溢出。
        var scaled = new Vector2(horizontalWidth / largest, verticalWidth / largest);
        return scaled / scaled.Length();
    }

    private static float GetOppositeSplit(float size, float firstWidth, float secondWidth)
    {
        var largest = MathF.Max(firstWidth, secondWidth);
        if (largest <= 0f)
            return size * 0.5f;

        var first = firstWidth / largest;
        var second = secondWidth / largest;
        return size * (first / (first + second));
    }

    internal readonly record struct BorderColorPartition(
        Vector4 TopCornerWeights, Vector4 BottomCornerWeights, Vector2 OppositeSplits, Vector4 EnabledSides);

    /// <summary>
    /// 为已经构建的顶点填写 UV，将指定区域映射到整个外扩后的几何范围。
    /// uvOrigin 为左上角，uvSize 为归一化纹理坐标中的尺寸。
    /// </summary>
    internal static void WriteTextureCoordinates(Span<SDFGraphicsVertexType> destination,
        Vector2 uvOrigin, Vector2 uvSize)
    {
        var halfUvSize = uvSize * 0.5f;
        for (var quadrant = 0; quadrant < QuadrantCount; quadrant++)
        {
            var quadrantUv = uvOrigin + new Vector2(
                (quadrant & 1) * halfUvSize.X, (quadrant >> 1) * halfUvSize.Y);

            for (var vertex = 0; vertex < VerticesPerQuadrant; vertex++)
            {
                destination[quadrant * VerticesPerQuadrant + vertex].TextureCoordinates = quadrantUv + new Vector2(
                    (vertex & 1) * halfUvSize.X, (vertex >> 1) * halfUvSize.Y);
            }
        }
    }
}
