using SilkyUIFramework.Components;
using Terraria.UI.Chat;

namespace SilkyUIFramework.Elements;

[XmlElementMapping("TextView")]
public class UITextView : UIView
{
    public void UseDeathText() => Font = FontAssets.DeathText.Value;
    public void UseMouseText() => Font = FontAssets.MouseText.Value;
    public bool IsDeathText => Font == FontAssets.DeathText.Value;
    public bool IsMouseText => Font == FontAssets.MouseText.Value;

    #region 控制属性

    public virtual DynamicSpriteFont Font
    {
        get => field ?? FontAssets.MouseText.Value;
        set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    /// <summary> 当输入内容更改时触发 </summary>
    public event ContentChangingEventHandler ContentChanging;

    /// <summary> 当内容更改后触发 </summary>
    public event ContentChangedEventHandler ContentChanged;

    /// <summary> 当输入内容更改时触发 </summary>
    /// <returns>新值</returns>
    protected virtual string OnContentChanging(string newText, string oldText)
    {
        return newText;
    }

    /// <summary> 当内容更改后触发 </summary>
    protected virtual void OnContentChanged(string text) { }

    /// <summary> 最大字符，只在输入时生效。 </summary>
    public int MaximumCharacters
    {
        get; set
        {
            if (field == value) return;
            field = value;
            if (field > 0 && Text.Length > field) Text = Text[..field];
        }
    }

    public virtual string Text
    {
        get; set
        {
            if (value is null) return;
            if (value.Equals(field)) return;
            if (MaximumCharacters > 0 && value.Length > MaximumCharacters) value = value[..MaximumCharacters];

            var changingEventArgs = new ContentChangingEventArgs(value, field);
            ContentChanging?.Invoke(this, changingEventArgs);

            value = OnContentChanging(value, field);

            if (value.Equals(field)) return;

            field = value;
            MarkLayoutDirty();

            var changedEventArgs = new ContentChangedEventArgs(field);
            ContentChanged?.Invoke(this, changedEventArgs);
            OnContentChanged(field);
        }
    } = string.Empty;

    /// <summary> 是否自动换行 </summary>
    public bool WordWrap
    {
        get; set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    }

    public int MaxLines
    {
        get; set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    } = -1;

    public float TextScale
    {
        get; set
        {
            if (field == value) return;
            field = value;
            MarkLayoutDirty();
        }
    } = 1f;

    public Color TextColor { get; set; } = Color.White;
    public float TextBorder { get; set; } = 2f;
    public Color TextBorderColor { get; set; } = Color.Black;

    public Vector2 TextOffset { get; set; } = Vector2.Zero;
    public Vector2 TextPercentOffset { get; set; } = Vector2.Zero;
    public Vector2 TextPercentOrigin { get; set; } = Vector2.Zero;
    public Vector2 TextAlign { get; set; } = Vector2.Zero;
    public bool IgnoreTextColor { get; set; } = false;

    #endregion

    protected readonly List<TextSnippet> IntermediateSnippets = [];

    protected SnippetModule SnippetModule { get; } = new();

    public UITextView()
    {
        FitWidth = true;
        FitHeight = true;
    }

    public Vector2 TextSize { get; protected set; } = Vector2.Zero;

    public override void Measure(float width, float height)
    {
        UpdateWidthConstraints(width);
        UpdateHeightConstraints(height);

        if (FitWidth)
        {
            RecalculateString(WidthMertrics.MinInner);
            SetInnerBoundsWidthRaw(WidthMertrics.ClampInner(TextSize.X * TextScale));
        }
        else
        {
            UpdateBoundsWidth(width);
            RecalculateString(InnerBounds.Width);
        }

        if (FitHeight)
        {
            SetInnerBoundsHeightRaw(HeightMertrics.ClampInner(TextSize.Y * TextScale));
        }
        else UpdateBoundsHeight(height);
    }

    public override void RecalculateHeight()
    {
        RecalculateString(InnerBounds.Width);

        if (FitHeight) SetInnerBoundsHeightRaw(HeightMertrics.ClampInner(TextSize.Y * TextScale));
    }

    protected virtual void RecalculateString(float maxWidth)
    {
        IntermediateSnippets.Parse(Text, Color.White).ConvertPlainSnippet();

        SnippetModule.UpdateProperties(Font, maxWidth, MaxLines);

        if (WordWrap)
        {
            SnippetModule.WordWrapSnippets(IntermediateSnippets);
        }
        else
        {
            SnippetModule.FromSnippets(IntermediateSnippets);
        }

        TextSize = SnippetModule.GetStringSize(Font, new Vector2(1f));
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        DrawSnippets(spriteBatch);
    }

    protected virtual void DrawSnippets(SpriteBatch spriteBatch)
    {
        var innerSize = (Vector2)InnerBounds.Size;

        var textSize = TextSize * TextScale;

        var textPosition = InnerBounds.Position + TextOffset + TextPercentOffset * innerSize
            + TextAlign * (innerSize - textSize) - TextPercentOrigin * textSize;
        textPosition.Y += TextScale * GetFontOffset();

        SnippetModule.DrawTextShadow(spriteBatch, Font, textPosition, TextBorderColor, 0f, Vector2.Zero, new(TextScale), TextBorder);
        SnippetModule.DrawText(spriteBatch, Font, textPosition, TextColor, 0f, Vector2.Zero, new(TextScale), out var snippet, IgnoreTextColor);
        snippet?.OnHover();
    }

    protected virtual float GetFontOffset() => TextDrawingHelper.GetFontOffset(Font);
}

public class ContentChangingEventArgs(string newText, string oldText) : EventArgs
{
    public string NewText { get; set; } = newText;
    public string OldText { get; } = oldText;
}

public class ContentChangedEventArgs(string text) : EventArgs
{
    public string Text { get; } = text;
}

public delegate string ContentChangingEventHandler(UITextView sender, ContentChangingEventArgs e);

public delegate void ContentChangedEventHandler(UITextView sender, ContentChangedEventArgs e);