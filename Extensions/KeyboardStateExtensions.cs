using Microsoft.Xna.Framework.Input;

namespace SilkyUIFramework.Extensions;

public static class KeyboardStateExtensions
{
    extension(ref KeyboardState keyboardState)
    {
        public bool IsControlKeyDown =>
            keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);

        public bool IsAltKeyDown =>
            keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

        public bool IsShiftKeyDown =>
            keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        /// <summary>
        /// 刚刚按下
        /// </summary>
        public static bool JustPressed(Keys key) =>
            Main.inputText.IsKeyDown(key) && Main.oldInputText.IsKeyUp(key);

        /// <summary>
        /// 刚刚松开
        /// </summary>
        public static bool JustReleased(Keys key) =>
            Main.inputText.IsKeyUp(key) && Main.oldInputText.IsKeyDown(key);
    }
}