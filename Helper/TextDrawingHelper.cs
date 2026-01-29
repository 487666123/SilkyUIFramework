namespace SilkyUIFramework.Helper;

public static class TextDrawingHelper
{
    public static float DeathTextOffset { get; internal set; }
    public static float MouseTextOffset { get; internal set; }

    public static float GetFontOffset(DynamicSpriteFont font)
    {
        if (font == FontAssets.DeathText.Value) return DeathTextOffset;
        return font == FontAssets.MouseText.Value ? MouseTextOffset : 0f;
    }
}
