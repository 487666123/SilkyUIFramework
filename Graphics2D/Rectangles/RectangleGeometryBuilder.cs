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
