namespace SilkyUIFramework;

public class RenderStates
{
    private RenderStates() { }

    /// <summary>
    /// ���ģʽ
    /// </summary>
    public BlendState BlendState { get; private set; }

    /// <summary>
    /// ����״̬
    /// </summary>
    public SamplerState SamplerState { get; private set; }

    /// <summary>
    /// ���
    /// </summary>
    public DepthStencilState DepthStencilState { get; private set; }

    /// <summary>
    /// ��դ��
    /// </summary>
    public RasterizerState RasterizerState { get; private set; }
    public Matrix Matrix { get; private set; }

    public void Begin(SpriteBatch spriteBatch, SpriteSortMode spriteSortMode, Effect effect = null, Matrix? matrix = null)
    {
        spriteBatch.Begin(spriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, effect, matrix ?? Matrix);
    }

    public static RenderStates BackupStates(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        return new RenderStates
        {
            BlendState = device.BlendState,
            SamplerState = device.SamplerStates[0],
            DepthStencilState = device.DepthStencilState,
            RasterizerState = device.RasterizerState,
            Matrix = spriteBatch.transformMatrix,
        };
    }
}