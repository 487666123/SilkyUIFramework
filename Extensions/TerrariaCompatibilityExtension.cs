namespace SilkyUIFramework.Extensions;

/// <summary>
/// 泰拉兼容性扩展
/// </summary>
public static class TerrariaCompatibilityExtension
{
    extension(GameTime gameTime)
    {
        /// <summary>
        /// 跳帧兼容
        /// </summary>
        public double TerrariaTotalSeconds
        {
            get
            {
                return Main.FrameSkipMode == Terraria.Enums.FrameSkipMode.Subtle ?
                    1f / 60f : gameTime.ElapsedGameTime.TotalSeconds;
            }
        }
    }
}
