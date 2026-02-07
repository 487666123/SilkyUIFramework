using Microsoft.Xna.Framework.Input;
using Terraria.UI.Chat;

namespace SilkyUIFramework.Elements;

[XmlElementMapping("EditText")]
public class SUIEditText : UITextView
{

    public Color CursorColor = Color.White;
    public Color CursorFlashColor { get; set; }

    /// <summary> 别问为什么是 2 </summary>
    protected readonly List<TextSnippet> IntermediateSnippets2 = [];

    public string Placeholder
    {
        get; set
        {
            if (value == null) return;
            if (value.Equals(field)) return;
            field = value;
            if (string.IsNullOrEmpty(Text))
                MarkLayoutDirty();
        }
    } = string.Empty;


    public Color PlaceholderColor { get; set; } = Color.Gray;
    public Color PlaceholderBorderColor { get; set; } = Color.Black;

    public SUIEditText()
    {
        OccupyPlayerInput = true;
        CursorSnippet = new CursorSnippet(this);
    }

    public readonly CursorSnippet CursorSnippet;

    protected override void RecalculateString(float maxWidth)
    {
        var text = Text.Length == 0 ? Placeholder : Text;

        var beforeText = text[..CursorIndex];
        var afterText = text[CursorIndex..];

        IntermediateSnippets.Parse(beforeText, Color.White).ConvertPlainSnippet();
        IntermediateSnippets2.Parse(afterText, Color.White).ConvertPlainSnippet();

        IntermediateSnippets.Add(CursorSnippet);
        IntermediateSnippets.AddRange(IntermediateSnippets2);

        SnippetModule.UpdateProperties(Font, maxWidth, MaxLines);

        if (WordWrap) SnippetModule.WordWrapSnippets(IntermediateSnippets);
        else SnippetModule.FromSnippets(IntermediateSnippets);

        TextSize = SnippetModule.GetStringSize(Font, Vector2.One);
    }

    protected override void DrawSnippets(SpriteBatch spriteBatch)
    {
        var innerSize = (Vector2)InnerBounds.Size;

        var textSize = TextSize * TextScale;

        var textPosition = InnerBounds.Position + TextOffset + TextPercentOffset * innerSize
            + TextAlign * (innerSize - textSize) - TextPercentOrigin * textSize;
        textPosition.Y += TextScale * GetFontOffset();

        var borderColor = string.IsNullOrEmpty(Text) ? PlaceholderBorderColor : TextBorderColor;
        var color = string.IsNullOrEmpty(Text) ? PlaceholderColor : TextColor;

        SnippetModule.DrawTextShadow(spriteBatch, Font, textPosition, borderColor, 0f, Vector2.Zero, new(TextScale), TextBorder);
        SnippetModule.DrawText(spriteBatch, Font, textPosition, color, 0f, Vector2.Zero, new(TextScale), out var snippet, IgnoreTextColor);
        snippet?.OnHover();
    }

    private double _cursorFlashTimer;
    public double CursorCycle { get; set; } = 2;

    /// <summary>
    /// 光标设置到最亮状态
    /// </summary>
    public void CursorToBrightest() => _cursorFlashTimer = CursorCycle / 2;

    protected override void UpdateStatus(GameTime gameTime)
    {
        base.UpdateStatus(gameTime);

        if (IsFocus)
        {
            var half = CursorCycle / 2;
            if (_cursorFlashTimer < half)
            {
                CursorFlashColor = CursorColor * (float)(_cursorFlashTimer / half);
            }
            else
            {
                CursorFlashColor = CursorColor * (float)(2 - _cursorFlashTimer / half);
            }

            _cursorFlashTimer += gameTime.TerrariaTotalSeconds;
            _cursorFlashTimer %= CursorCycle;
        }
        else CursorFlashColor = Color.Transparent;
    }

    private int _cursorIndex;

    public int CursorIndex
    {
        get => MathHelper.Clamp(_cursorIndex, 0, Text.Length);
        set
        {
            if (_cursorIndex == value) return;
            _cursorIndex = value;
            MarkLayoutDirty();
        }
    }

    public event Action OnEnterKeyDown;

    public override void HandlePlayerInput(bool inputMethodStatus)
    {
        // 当前键盘状态
        var keyboardState = Main.inputText;

        var inputString = string.Empty;

        // Ctrl
        // 裁剪，复制，粘贴
        if (keyboardState.IsControlKeyDown)
        {
            // 剪切
            if (KeyboardState.JustPressed(Keys.X))
            {
                KeyboardInputHelper.SetClipboard(Text);
                Text = "";
            }
            // 复制
            else if (KeyboardState.JustPressed(Keys.C))
            {
                KeyboardInputHelper.SetClipboard(Text);
            }
            // 粘贴
            else if (KeyboardState.JustPressed(Keys.V))
            {
                inputString = Main.PasteTextIn(true, inputString);
            }
        }
        // Alt
        else if (!keyboardState.IsAltKeyDown)
        {
            inputString = KeyboardInputHelper.GetPlayerInput();
        }

        // 换行 => Shift + Enter
        var lineBreak = KeyboardState.JustPressed(Keys.Enter) && keyboardState.IsShiftKeyDown;

        if (lineBreak) inputString += "\n";
        else if (keyboardState.IsKeyDown(Keys.Enter))
            inputString = string.Empty;

        if (inputMethodStatus)
        {
            _longPressBackSpaceTimer = 0;
        }
        else
        {
            UpdateBackSpace();
        }

        InsertText(inputString);

        UpdateCursorMovement();

        if (KeyboardState.JustReleased(Keys.Enter) || lineBreak) return;

        OnEnterKeyDown?.Invoke();
    }

    private double _lastMoveCursorTime;
    private double _moveCursorTimer;

    /// <summary>
    /// 更新光标位置
    /// </summary>
    private void UpdateCursorMovement()
    {
        if (Main.inputText.IsKeyDown(Keys.Left) || Main.inputText.IsKeyDown(Keys.Right))
        {
            var interval = _cursorFlashTimer - _lastMoveCursorTime;

            switch (_moveCursorTimer)
            {
                case 0:
                    MoveCursor();
                    break;
                case >= 0.2 and < 0.5:
                    if (interval > 0.05)
                    {
                        MoveCursor();
                        _lastMoveCursorTime = _cursorFlashTimer;
                    }
                    break;
                case >= 0.5:
                    if (interval > 0.025)
                    {
                        MoveCursor();
                        _lastMoveCursorTime = _cursorFlashTimer;
                    }
                    break;
            }

            _moveCursorTimer = Math.Min(2, _moveCursorTimer + Main.gameTimeCache.TerrariaTotalSeconds);
        }
        else
        {
            _lastMoveCursorTime = 0;
            _moveCursorTimer = 0;
        }
    }

    /// 移动光标
    private void MoveCursor()
    {
        CursorToBrightest();

        if (Main.inputText.IsKeyDown(Keys.Left)) CursorIndex--;
        else if (Main.inputText.IsKeyDown(Keys.Right)) CursorIndex++;
    }

    private double _lastLongPressBackSpaceTime;
    private double _longPressBackSpaceTimer;

    /// <summary>
    /// 更新退格键
    /// </summary>
    public void UpdateBackSpace()
    {
        if (Main.inputText.IsKeyDown(Keys.Back))
        {
            var interval = _longPressBackSpaceTimer - _lastLongPressBackSpaceTime;
            switch (_longPressBackSpaceTimer)
            {
                case 0:
                    PerformBackspace();
                    break;
                case >= 0.2 and < 0.5:
                    if (interval > 0.05)
                    {
                        PerformBackspace();
                        _lastLongPressBackSpaceTime = _longPressBackSpaceTimer;
                    }
                    break;
                case >= 0.5:
                    if (interval > 0.025)
                    {
                        PerformBackspace();
                        _lastLongPressBackSpaceTime = _longPressBackSpaceTimer;
                    }
                    break;
            }

            _longPressBackSpaceTimer += Main.gameTimeCache.TerrariaTotalSeconds;
        }
        else
        {
            _lastLongPressBackSpaceTime = 0;
            _longPressBackSpaceTimer = 0;
        }
    }

    /// <summary>
    /// 执行退格
    /// </summary>
    private void PerformBackspace()
    {
        CursorToBrightest();

        if (CursorIndex == 0) return;

        if (Text.Length > 0)
            Text = Text.Remove(--CursorIndex, 1);
    }

    /// 插入字符, 位置由光标决定 
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        CursorToBrightest();

        Text = Text.Insert(CursorIndex, text);
        CursorIndex += text.Length;
    }

    /// <summary>
    /// 当开始输入操作时触发
    /// </summary>
    public event MouseEventHandler StartTakingInput;

    /// <summary>
    /// 当结束输入操作时触发
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<string>> EndTakingInput;

    private string LastInputText { get; set; }

    #region 螺线的鼠标定位光标，先搁置。

    // 虽然和GotFocus事件定位重复了，但是我感觉还是专门整个事件来管理开始和结束输入比较好

    public override void OnGotFocus(UIMouseEvent evt)
    {
        base.OnGotFocus(evt);

        CursorToBrightest();
        RePositioningCursorIndex(evt.MousePosition);
        LastInputText = Text;
        StartTakingInput?.Invoke(this, evt);
    }

    public override void OnLostFocus(UIMouseEvent evt)
    {
        base.OnLostFocus(evt);
        EndTakingInput?.Invoke(this, new(LastInputText, Text));
    }

    void RePositioningCursorIndex(Vector2 mousePosition)
    {
        if (mousePosition.X > Bounds.X + Bounds.Width)
            CursorIndex = Text.Length;
        else if (mousePosition.X < Bounds.X)
            CursorIndex = 0;
        else
        {
            // TODO 螺线瞎写的定位，应该要改

            //int resultIndex = 0;

            var innerSize = (Vector2)InnerBounds.Size;

            var textSize = TextSize;
            // 无字符时会出问题，加上这行就好了
            textSize.Y = Math.Max(Font.LineSpacing, textSize.Y);

            var textPos =
                InnerBounds.Position
                + TextOffset
                + TextPercentOffset * innerSize
                + TextAlign * (innerSize - textSize * TextScale)
                - TextPercentOrigin * TextSize * TextScale;
            var fontOffset = GetFontOffset();
            textPos.Y += TextScale * fontOffset;

            var textLength = Text.Length;
            var n = 0;
            for (; textPos.X < mousePosition.X && n < textLength; n++)
                textPos.X += Font.MeasureString(Text[n].ToString()).X * TextScale;

            CursorIndex = n;
        }
    }

    #endregion
}