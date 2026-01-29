using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SilkyUIFramework.Configs;

public class SilkyUIClientConfig : ModConfig
{
    public static SilkyUIClientConfig Instance => ModContent.GetInstance<SilkyUIClientConfig>();

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Slider]
    [DefaultValue(5.5f)]
    [Range(0, 10f)]
    [Increment(0.25f)]
    [CustomModConfigItem(typeof(MouseTextOffsetPreview))]
    public float MouseTextOffset;

    [Slider]
    [DefaultValue(15.5f)]
    [Range(0, 25f)]
    [Increment(0.25f)]
    [CustomModConfigItem(typeof(DeathTextOffsetPreview))]
    public float DeathTextOffset;

    //[Header("Blur Settings")]
    [DefaultValue(true)]
    public bool EnableBlur;

    [DefaultValue(false)]
    public bool SingleBlur;

    [Slider]
    [DefaultValue(2f)]
    [Range(1f, 8f)]
    [Increment(0.5f)]
    public float BlurZoomMultiplierDenominator;

    [Slider]
    [DefaultValue(2)]
    [Range(0, 10)]
    [Increment(1)]
    public int BlurIterationCount;

    [Slider]
    [DefaultValue(2f)]
    [Range(1f, 10f)]
    [Increment(1f)]
    public float IterationOffsetMultiplier;

    [Slider]
    [DefaultValue(BlurMixingNumber.Three)]
    public BlurMixingNumber BlurMixingNumber;

    public override void OnChanged()
    {
        TextDrawingHelper.DeathTextOffset = DeathTextOffset;
        TextDrawingHelper.MouseTextOffset = MouseTextOffset;

        BlurMakeSystem.EnableBlur = EnableBlur;
        BlurMakeSystem.SingleBlur = SingleBlur;
        BlurMakeSystem.BlurZoomMultiplierDenominator = BlurZoomMultiplierDenominator;
        BlurMakeSystem.BlurIterationCount = BlurIterationCount;
        BlurMakeSystem.IterationOffsetMultiplier = IterationOffsetMultiplier;
        BlurMakeSystem.BlurMixingNumber = BlurMixingNumber;
    }
}