namespace SilkyUIFramework.Graphics2D;

/// <summary>
/// 负责构建 SDF 矩形绘制的顶点与索引数据。
/// 使用静态顶点缓冲复用内存，适用于主线程串行绘制路径。
/// </summary>
internal static class SDFRectangleGeometryBuilder
{
    /// <summary>
    /// 4 个象限共 16 个顶点缓存（每个象限 4 顶点）。
    /// 每次绘制都会覆写该数组。
    /// </summary>
    internal static readonly SDFGraphicsVertexType[] RectangleVertexData = new SDFGraphicsVertexType[16];

    /// <summary>
    /// 对应 4 个象限的索引数据（每象限 2 个三角形）。
    /// </summary>
    internal static readonly short[] IndexData = [0, 1, 2, 2, 1, 3, 4, 5, 6, 6, 5, 7, 8, 9, 10, 10, 9, 11, 12, 13, 14, 14, 13, 15];

    /// <summary>
    /// 构建纯色矩形绘制所需顶点数据。
    /// <paramref name="edgePadding"/> 用于按缩放补偿边缘，避免描边/抗锯齿出现像素缝隙。
    /// </summary>
    internal static void SetRectanglePrimitives(float edgePadding, Vector2 position, Vector2 size, Vector4 borderRadius)
    {
        position -= new Vector2(edgePadding);
        size += new Vector2(edgePadding * 2);
        borderRadius += new Vector4(edgePadding);

        size /= 2f;

        var vertexData = RectangleVertexData;

        vertexData.SetPosition(position, size, 0);
        vertexData.SetPosition(new(position.X + size.X, position.Y), size, 4);
        vertexData.SetPosition(new(position.X, position.Y + size.Y), size, 8);
        vertexData.SetPosition(position + size, size, 12);

        vertexData.SetDistanceFromEdge(size, borderRadius.X + edgePadding, 0, 1, 2, 3, 0);
        vertexData.SetDistanceFromEdge(size, borderRadius.Y + edgePadding, 1, 0, 3, 2, 4);
        vertexData.SetDistanceFromEdge(size, borderRadius.Z + edgePadding, 2, 3, 0, 1, 8);
        vertexData.SetDistanceFromEdge(size, borderRadius.W + edgePadding, 3, 2, 1, 0, 12);

        vertexData.SetBorderRadius(borderRadius.X, 0);
        vertexData.SetBorderRadius(borderRadius.Y, 4);
        vertexData.SetBorderRadius(borderRadius.Z, 8);
        vertexData.SetBorderRadius(borderRadius.W, 12);
    }

    /// <summary>
    /// 构建带纹理坐标的矩形绘制顶点数据。
    /// 输入 UV 采用矩形左上 + 尺寸形式。
    /// </summary>
    internal static void SetRectanglePrimitives(float edgePadding, Vector2 position, Vector2 size, Vector4 borderRadius,
        Vector2 textureCoordinatesPosition, Vector2 textureCoordinatesSize)
    {
        position -= new Vector2(edgePadding);
        size += new Vector2(edgePadding * 2);
        borderRadius += new Vector4(edgePadding);

        size /= 2f;
        textureCoordinatesSize /= 2f;

        var vertexData = RectangleVertexData;

        vertexData.SetPosition(position, size, 0);
        vertexData.SetPosition(new(position.X + size.X, position.Y), size, 4);
        vertexData.SetPosition(new(position.X, position.Y + size.Y), size, 8);
        vertexData.SetPosition(position + size, size, 12);

        vertexData.SetTextureCoordinates(textureCoordinatesPosition, textureCoordinatesSize, 0);
        vertexData.SetTextureCoordinates(new(textureCoordinatesPosition.X + textureCoordinatesSize.X, textureCoordinatesPosition.Y), textureCoordinatesSize, 4);
        vertexData.SetTextureCoordinates(new(textureCoordinatesPosition.X, textureCoordinatesPosition.Y + textureCoordinatesSize.Y), textureCoordinatesSize, 8);
        vertexData.SetTextureCoordinates(textureCoordinatesPosition + textureCoordinatesSize, textureCoordinatesSize, 12);

        vertexData.SetDistanceFromEdge(size, borderRadius.X + edgePadding, 0, 1, 2, 3, 0);
        vertexData.SetDistanceFromEdge(size, borderRadius.Y + edgePadding, 1, 0, 3, 2, 4);
        vertexData.SetDistanceFromEdge(size, borderRadius.Z + edgePadding, 2, 3, 0, 1, 8);
        vertexData.SetDistanceFromEdge(size, borderRadius.W + edgePadding, 3, 2, 1, 0, 12);

        vertexData.SetBorderRadius(borderRadius.X, 0);
        vertexData.SetBorderRadius(borderRadius.Y, 4);
        vertexData.SetBorderRadius(borderRadius.Z, 8);
        vertexData.SetBorderRadius(borderRadius.W, 12);
    }

    /// <summary>
    /// 按一个象限写入 4 个顶点位置。
    /// </summary>
    private static void SetPosition(this SDFGraphicsVertexType[] vertexData, Vector2 position, Vector2 size, int indexOffset)
    {
        vertexData[indexOffset].Position = position;
        vertexData[indexOffset + 1].Position = new Vector2(position.X + size.X, position.Y);
        vertexData[indexOffset + 2].Position = new Vector2(position.X, position.Y + size.Y);
        vertexData[indexOffset + 3].Position = position + size;
    }

    /// <summary>
    /// 为一个象限的 4 个顶点写入相同圆角半径。
    /// </summary>
    private static void SetBorderRadius(this SDFGraphicsVertexType[] vertexData, float borderRadius, int indexOffset)
    {
        vertexData[indexOffset].BorderRadius = borderRadius;
        vertexData[indexOffset + 1].BorderRadius = borderRadius;
        vertexData[indexOffset + 2].BorderRadius = borderRadius;
        vertexData[indexOffset + 3].BorderRadius = borderRadius;
    }

    /// <summary>
    /// 写入 SDF 距离字段，用于像素着色器计算圆角矩形距离场。
    /// <paramref name="a"/>..<paramref name="d"/> 定义该象限 4 个顶点的写入顺序映射。
    /// </summary>
    private static void SetDistanceFromEdge(this SDFGraphicsVertexType[] vertexData, Vector2 size, float borderRadius,
        int a, int b, int c, int d, int indexOffset)
    {
        vertexData[indexOffset + a].DistanceFromEdge = new Vector2(borderRadius);
        vertexData[indexOffset + b].DistanceFromEdge = new Vector2(borderRadius - size.X, borderRadius);
        vertexData[indexOffset + c].DistanceFromEdge = new Vector2(borderRadius, borderRadius - size.Y);
        vertexData[indexOffset + d].DistanceFromEdge = new Vector2(borderRadius) - size;
    }

    /// <summary>
    /// 为一个象限写入 4 个纹理坐标。
    /// </summary>
    private static void SetTextureCoordinates(this SDFGraphicsVertexType[] vertexData, Vector2 position, Vector2 size, int indexOffset)
    {
        vertexData[indexOffset].TextureCoordinates = position;
        vertexData[indexOffset + 1].TextureCoordinates = new Vector2(position.X + size.X, position.Y);
        vertexData[indexOffset + 2].TextureCoordinates = new Vector2(position.X, position.Y + size.Y);
        vertexData[indexOffset + 3].TextureCoordinates = position + size;
    }
}
