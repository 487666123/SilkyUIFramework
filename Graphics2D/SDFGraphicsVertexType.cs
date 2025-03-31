﻿namespace SilkyUIFramework.Graphics2D;

public struct SDFGraphicsVertexType(Vector2 position, Vector2 textureCoordinates, Vector2 dimensionCoordinates, float borderRadius) : IVertexType
{
    public Vector2 Position = position;
    public Vector2 TextureCoordinates = textureCoordinates;
    public Vector2 DimensionCoordinates = dimensionCoordinates;
    public float BorderRadius = borderRadius;

    private static readonly VertexDeclaration VertexDeclaration = new([
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate,1),
            new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2)
        ]);

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}