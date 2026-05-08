using ReLogic.Content;

namespace SilkyUIFramework.Interfaces;

public interface ISilkyUIAssetProvider
{
    Asset<Effect> BlurEffect { get; }
    Asset<Effect> SDFGraphics { get; }
    Asset<Effect> SDFRectangle { get; }
}
