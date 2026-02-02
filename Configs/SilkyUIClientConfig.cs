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
    public float MouseTextOffset { get; set; }

    [Slider]
    [DefaultValue(15.5f)]
    [Range(0, 25f)]
    [Increment(0.25f)]
    [CustomModConfigItem(typeof(DeathTextOffsetPreview))]
    public float DeathTextOffset { get; set; }

    //[Header("Blur Settings")]
    [DefaultValue(true)]
    public bool EnableBlur { get; set; }

    [DefaultValue(false)]
    public bool SingleBlur { get; set; }

    [Slider]
    [DefaultValue(2f)]
    [Range(1f, 8f)]
    [Increment(0.5f)]
    public float BlurZoomMultiplierDenominator { get; set; }

    [Slider]
    [DefaultValue(2)]
    [Range(0, 10)]
    [Increment(1)]
    public int BlurIterationCount { get; set; }

    [Slider]
    [DefaultValue(2f)]
    [Range(1f, 10f)]
    [Increment(1f)]
    public float IterationOffsetMultiplier { get; set; }

    [Slider]
    [DefaultValue(BlurMixingNumber.Three)]
    public BlurMixingNumber BlurMixingNumber { get; set; }

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