namespace SilkyUIFramework;

public class GraphicsDeviceStates
{
    private GraphicsDeviceStates() { }

    private SpriteBatch SpriteBatch { get; init; }

    /// <summary>
    /// 混合模式
    /// </summary>
    private BlendState BlendState { get; init; }

    /// <summary>
    /// 采样状态
    /// </summary>
    private SamplerState SamplerState { get; init; }

    /// <summary>
    /// 深度
    /// </summary>
    private DepthStencilState DepthStencilState { get; init; }

    /// <summary>
    /// 光栅化
    /// </summary>
    private RasterizerState RasterizerState { get; init; }

    private Matrix Matrix { get; init; }

    /// <summary>
    /// 使用保存的状态 Begin
    /// </summary>
    public void Begin(SpriteSortMode spriteSortMode = 0, Effect effect = null, Matrix? matrix = null)
    {
        SpriteBatch.Begin(spriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, effect, matrix ?? Matrix);
    }

    public static GraphicsDeviceStates BackupStates(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        return new GraphicsDeviceStates
        {
            SpriteBatch = spriteBatch,
            BlendState = device.BlendState,
            SamplerState = device.SamplerStates[0],
            DepthStencilState = device.DepthStencilState,
            RasterizerState = device.RasterizerState,
            Matrix = spriteBatch.transformMatrix,
        };
    }
}