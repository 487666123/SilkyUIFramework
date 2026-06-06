using Terraria.UI.Chat;

namespace SilkyUIFramework;

/// <summary>
/// 输入框光标
/// </summary>
public class CursorSnippet(SUIEditText editText) : TextSnippet(" ")
{
    private readonly SUIEditText _editText = editText;
    public DynamicSpriteFont Font { get; set; }

    /// <summary>
    /// 特殊适配高度，我也不想这么写，但是这个底层架构我还能怎么办，啊啊啊啊啊啊啊啊啊
    /// </summary>
    public float TrueHeight { get; set; }

    // 宽度计算
    public override float GetStringLength(DynamicSpriteFont font) => 0f;

    // 原版的 Snippet 使用 Draw 方法来计算大小
    // 用一个 bool justCheckingString 参数来区分是在计算还是在绘制
    // 如果你只是在计算大小, 也需要传入一堆无关的绘制参数
    // 你也许会想这堆参数或许会影响计算结果？实际他们自己并没有这做过
    // 并且如果真让绘制参数来影响计算结果, 那也是一个很差的实践, 所以他们不该这样做, 也不该让我们考虑这件事
    public override bool UniqueDraw(bool justCheckingString,
        out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1)
    {
        if (Font == null)
        {
            size = Vector2.Zero;
            return true;
        }

        size = new Vector2(0, TrueHeight);

        if (!justCheckingString)
        {
            position.Y -= TextDrawingHelper.GetFontOffset(Font) * scale;

            spriteBatch?.Draw(TextureAssets.MagicPixel.Value, position,
                new Rectangle(0, 0, 1, 1), _editText.CursorFlashColor, 0f, Vector2.Zero,
                new Vector2(2f, size.Y), SpriteEffects.None, 0f);
        }

        return true;
    }
}