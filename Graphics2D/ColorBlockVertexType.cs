// namespace SilkyUIFramework.Graphics2D;
//
// public struct ColorBlockVertexType : IVertexType
// {
//     public Vector2 Position;
//     public Color Color;
//
//     public ColorBlockVertexType(Vector2 position, Color color)
//     {
//         Position = position;
//         Color = color;
//     }
//
//     public ColorBlockVertexType(float x, float y, Color color)
//     {
//         Position.X = x;
//         Position.Y = y;
//         Color = color;
//     }
//
//     public VertexDeclaration VertexDeclaration { get; } = new(
//         new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
//         new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0));
// }
//
// /// <summary>
// /// 合批
// /// </summary>
// public class EffectBatch(GraphicsDevice graphicsDevice)
// {
//     public bool IsBegin { get; private set; }
//
//     public GraphicsDevice GraphicsDevice { get; private set; } = graphicsDevice;
//     public BlendState BlendState { get; private set; }
//     public SamplerState SamplerState { get; private set; }
//     public DepthStencilState DepthStencilState { get; private set; }
//     public RasterizerState RasterizerState { get; private set; }
//     public Effect Effect { get; private set; }
//     public Matrix TransformMatrix { get; private set; }
//
//     public uint CurrentIndex;
//     private ColorBlockVertexType[] _vertices;
//
//     public void Begin(BlendState blendState, SamplerState samplerState,
//         DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
//     {
//         if (IsBegin)
//         {
//             throw new InvalidOperationException("Begin must be called before calling Begin.");
//         }
//
//         BlendState = blendState;
//         SamplerState = samplerState;
//         DepthStencilState = depthStencilState;
//         RasterizerState = rasterizerState;
//         Effect = effect;
//         TransformMatrix = transformMatrix;
//
//         IsBegin = true;
//     }
//
//     public void Draw(ColorBlockVertexType vertexType)
//     {
//         var a = new VertexBuffer(Main.graphics.graphicsDevice, GetType(), 3, BufferUsage.None);
//         a.SetData([vertexType, vertexType, vertexType]);
//
//         if (CurrentIndex >= _vertices.Length)
//         {
//             Array.Resize(ref _vertices, _vertices.Length * 2);
//         }
//
//         _vertices[CurrentIndex] = vertexType;
//         CurrentIndex++;
//     }
//
//     public void End()
//     {
//         if (!IsBegin)
//         {
//             throw new InvalidOperationException("End must be called before calling End.");
//         }
//
//
//         IsBegin = false;
//     }
//
//     public void PrepRenderState()
//     {
//         var device = GraphicsDevice;
//         device.BlendState = BlendState;
//         device.SamplerStates[0] = SamplerState;
//         device.DepthStencilState = DepthStencilState;
//         device.RasterizerState = RasterizerState;
//
//         Effect.CurrentTechnique.Passes[0].Apply();
//     }
// }