using ReLogic.Content;
using SilkyUIFramework.Interfaces;

namespace SilkyUIFramework;

internal class SilkyUIAssetProvider : ISilkyUIAssetProvider
{
    Asset<Effect> ISilkyUIAssetProvider.BlurEffect => field ??= ModAsset.BlurEffect;

    Asset<Effect> ISilkyUIAssetProvider.SDFGraphics => field ??= ModAsset.SDFGraphics;

    Asset<Effect> ISilkyUIAssetProvider.SDFRectangle => field ??= ModAsset.SDFRectangle;
}
