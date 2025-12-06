using Microsoft.Xna.Framework.Input;
using ReLogic.OS;

namespace SilkyUIFramework.Helper;

public static class KeyboardInputHelper
{
    public static string GetPlayerInput()
    {
        Main.inputTextEnter = false;
        Main.inputTextEscape = false;

        var text = "";

        for (var i = 0; i < Main.keyCount; i++)
        {
            var key = (Keys)Main.keyInt[i]; // 键值
            var keyString = Main.keyString[i]; // 键对应的字符
            switch (key)
            {
                case Keys.Enter:
                {
                    Main.inputTextEnter = true;
                    break;
                }
                case Keys.Escape:
                {
                    Main.inputTextEscape = true;
                    break;
                }
                case >= Keys.Space and not Keys.F16:
                {
                    text += keyString;
                    break;
                }
            }
        }

        Main.keyCount = 0;

        return text;
    }

    public static void SetClipboard(string text) => Platform.Get<IClipboard>().Value = text;
}