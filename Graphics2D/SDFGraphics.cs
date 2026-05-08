namespace SilkyUIFramework.Graphics2D;

public static class SDFGraphics
{
    private struct VertexPosCoordinate(Vector2 position, Vector2 coordinate) : IVertexType
    {
        public Vector2 Position = position;
        public Vector2 Coordinate = coordinate;

        readonly VertexDeclaration IVertexType.VertexDeclaration => new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
    }

    /// <summary> 生成矩形顶点 </summary>
    private static VertexPosCoordinate[] GenerateRectangle(Vector2 pos, Vector2 size)
    {
        var vertices = new VertexPosCoordinate[6];
        vertices[0] = new VertexPosCoordinate(pos, new Vector2(0, 0));
        vertices[1] = new VertexPosCoordinate(pos + new Vector2(size.X, 0), new Vector2(size.X, 0));
        vertices[2] = new VertexPosCoordinate(pos + new Vector2(0, size.Y), new Vector2(0, size.Y));
        vertices[3] = new VertexPosCoordinate(pos + new Vector2(0, size.Y), new Vector2(0, size.Y));
        vertices[4] = new VertexPosCoordinate(pos + new Vector2(size.X, 0), new Vector2(size.X, 0));
        vertices[5] = new VertexPosCoordinate(pos + size, size);
        return vertices;
    }

    private static void DrawRectanglePrimitives(Vector2 position, Vector2 size)
    {
        var vertices = GenerateRectangle(position, size);
        Main.graphics.GraphicsDevice.DrawUserPrimitives(0, vertices, 0, vertices.Length / 3);
        Main.spriteBatch.spriteEffectPass.Apply();
    }

    /// <summary>
    /// 统一准备 SDF Effect：矩阵转换到 SDF 空间并写入 uTransform。
    /// </summary>
    private static Effect PrepareEffect(Matrix matrix)
    {
        matrix = MatrixHelper.Transform2SDFMatrix(matrix);

        var effect = SilkyUISystem.Instance.AssetProvider.SDFGraphics.Value;
        effect.Parameters["uTransform"].SetValue(matrix);
        return effect;
    }

    /// 绘制叉号
    public static void HasBorderCross(Vector2 position, Vector2 size, float borderRadius, Color backgroundColor,
        float border, Color borderColor, Matrix matrix)
    {
        var effect = PrepareEffect(matrix);
        effect.Parameters["uSizeOver2"].SetValue(size / 2f);
        effect.Parameters["uBorder"].SetValue(border);
        effect.Parameters["uRound"].SetValue(borderRadius);
        effect.Parameters["uBorderColor"].SetValue(borderColor.ToVector4());
        effect.Parameters["uBackgroundColor"].SetValue(backgroundColor.ToVector4());
        const float root2Over2 = 1.414213562373f / 2f;
        effect.Parameters["uSmoothstepRange"].SetValue(new Vector2(-root2Over2, root2Over2) / Main.UIScale);
        effect.CurrentTechnique.Passes["HasBorderCross"].Apply();
        DrawRectanglePrimitives(position, size);
    }

    public static void HasBorderRound(Vector2 position, float size, Color background, float border, Color borderColor,
        Matrix matrix)
    {
        var effect = PrepareEffect(matrix);
        effect.Parameters["uBackgroundColor"].SetValue(background.ToVector4());
        effect.Parameters["uBorder"].SetValue(border);
        effect.Parameters["uBorderColor"].SetValue(borderColor.ToVector4());
        effect.CurrentTechnique.Passes["HasBorderRound"].Apply();
        DrawRectanglePrimitives(position, new Vector2(size));
    }

    public static void NoBorderRound(Vector2 position, float size, Color background, Matrix matrix)
    {
        var effect = PrepareEffect(matrix);
        effect.Parameters["uSizeOver2"].SetValue(new Vector2(size) / 2f);
        effect.Parameters["uBackgroundColor"].SetValue(background.ToVector4());
        effect.CurrentTechnique.Passes["NoBorderRound"].Apply();
        DrawRectanglePrimitives(position, new Vector2(size));
    }

    /// <summary>
    /// 绘制一条线，无边框
    /// </summary>
    public static void NoBorderLine(Vector2 topLef, Vector2 bottomRight, float width, Color background, Matrix matrix)
    {
        var min = Vector2.Min(topLef, bottomRight);
        var max = Vector2.Max(topLef, bottomRight);
        var size = max - min + new Vector2(width * 2);

        topLef += new Vector2(width) - min;
        bottomRight += new Vector2(width) - min;

        var effect = PrepareEffect(matrix);
        effect.Parameters["uStart"].SetValue(topLef);
        effect.Parameters["uEnd"].SetValue(bottomRight);
        effect.Parameters["uLineWidth"].SetValue(width);
        effect.Parameters["uBackgroundColor"].SetValue(background.ToVector4());
        effect.CurrentTechnique.Passes["NoBorderLine"].Apply();
        DrawRectanglePrimitives(min - new Vector2(width), size);
    }

    /// <summary>
    /// 绘制一条线，有边框
    /// </summary>
    public static void HasBorderLine(Vector2 topLef, Vector2 bottomRight, float width, Color background, float border,
        Color borderColor, Matrix matrix)
    {
        var min = Vector2.Min(topLef, bottomRight);
        var max = Vector2.Max(topLef, bottomRight);
        var size = max - min + new Vector2(width * 2);

        topLef += new Vector2(width) - min;
        bottomRight += new Vector2(width) - min;

        var effect = PrepareEffect(matrix);
        effect.Parameters["uStart"].SetValue(topLef);
        effect.Parameters["uEnd"].SetValue(bottomRight);
        effect.Parameters["uLineWidth"].SetValue(width);
        effect.Parameters["uBackgroundColor"].SetValue(background.ToVector4());
        effect.Parameters["uBorder"].SetValue(border);
        effect.Parameters["uBorderColor"].SetValue(borderColor.ToVector4());
        effect.CurrentTechnique.Passes["HasBorderLine"].Apply();
        DrawRectanglePrimitives(min - new Vector2(width), size);
    }
}