namespace SilkyUIFramework.Extensions;

internal static class SpriteBatchExtension
{
    extension(SpriteBatch spriteBatch)
    {
        public void ReBegin(SpriteSortMode sortMode,
            BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState,
            RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
        {
            spriteBatch.End();
            spriteBatch.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
        }
    }
}
