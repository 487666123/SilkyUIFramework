using Terraria.UI.Chat;

namespace SilkyUIFramework.Components;

public sealed class SnippetModule
{
    private List<SnippetLine> SnippetLines { get; } = [];
    public int Count => SnippetLines.Count;

    #region Properties

    private DynamicSpriteFont Font
    {
        get;
        set
        {
            if (value == null || field == value) return;
            field = value;
            _characterSpacing = field.CharacterSpacing;
        }
    }

    private float MaxWidth { get; set; }
    private int MaxLines { get; set; }

    public void UpdateProperties(DynamicSpriteFont font, float maxWidth, int maxLines)
    {
        Font = font;
        MaxWidth = maxWidth;
        MaxLines = maxLines;
    }

    #endregion

    private float _characterSpacing;
    private bool IsFull => MaxLines > 0 && SnippetLines.Count >= MaxLines;

    private bool TryCreateNewLine()
    {
        if (IsFull) return false;
        SnippetLines.Add(new SnippetLine());
        return true;
    }

    private bool EnoughSpace(float width)
    {
        var current = SnippetLines[^1];
        if (current.Count == 0) return true;
        return current.Width + _characterSpacing + width <= MaxWidth;
    }

    private bool TryAdd(TextSnippet snippet, float width)
    {
        var current = SnippetLines[^1];
        if (current.Count == 0)
        {
            current.Append(snippet, _characterSpacing, width);
            return true;
        }

        if (current.Width + _characterSpacing + width > MaxWidth)
        {
            if (!TryCreateNewLine())
                return false;
            current = SnippetLines[^1];
        }

        current.Append(snippet, _characterSpacing, width);
        return true;
    }

    private bool TryCommitToken(ref SnippetToken token)
    {
        if (token.Word == 0) return true;

        var width = token.Width;
        var snippet = token.ExtractSnippets();

        var current = SnippetLines[^1];
        if (current.Count == 0)
        {
            current.Append(snippet, _characterSpacing, width);
            return true;
        }

        if (current.Width + _characterSpacing + width > MaxWidth)
        {
            if (!TryCreateNewLine())
                return false;
            current = SnippetLines[^1];
        }

        current.Append(snippet, _characterSpacing, width);
        return true;
    }

    public void FromSnippets(List<TextSnippet> snippets)
    {
        SnippetLines.Clear();
        TryCreateNewLine();
        if (Font == null) return;

        var spacing = Font.CharacterSpacing;

        foreach (var snippet in snippets)
        {
            if (snippet is PlainSnippet)
            {
                var last = 0;
                var text = snippet.Text;
                var width = 0f;
                for (var i = 0; i < text.Length; i++)
                {
                    var c = text[i];
                    if (c.Equals('\n'))
                    {
                        if (last < i)
                            SnippetLines[^1].Append(snippet.Copy(text[last..i]), spacing, width);
                        if (!TryCreateNewLine()) return;
                        width = 0f;
                        last = i + 1;
                    }
                    else
                    {
                        if (width > 0) width += spacing;
                        width += Font.GetCharacterMetrics(c).KernedWidth;
                    }
                }

                if (last < text.Length)
                {
                    var part = text[last..];
                    SnippetLines[^1].Append(snippet.Copy(part), spacing, width);
                }
            }
            else
            {
                snippet.UniqueDraw(true, out var size, null);
                SnippetLines[^1].Append(snippet, spacing, size.X);
            }
        }
    }

    public void WordWrapSnippets(List<TextSnippet> snippets)
    {
        SnippetLines.Clear();
        if (Font == null) return;

        TryCreateNewLine();

        if (snippets is null || snippets.Count == 0) return;

        var font = Font;
        var token = new SnippetToken(MaxWidth, font.CharacterSpacing);

        foreach (var snippet in snippets)
        {
            if (snippet is CursorSnippet)
            {
                token.Snippets.Add(snippet);
                token.Append(0, false);
            }
            else if (snippet is PlainSnippet plainSnippet)
            {
                token.Snippets.Add(plainSnippet);

                foreach (var c in snippet.Text)
                {
                    var metrics = font.GetCharacterMetrics(c);

                    // 1. �жϵ�ǰ�ַ��Ƿ�Ϊ�հ��ַ�
                    var isWhiteSpace = char.IsWhiteSpace(c);

                    if (isWhiteSpace != token.IsWhiteSpace)
                    {
                        if (!TryCommitToken(ref token)) return;
                    }

                    token.IsWhiteSpace = isWhiteSpace;

                    if (isWhiteSpace)
                    {
                        if (c.Equals('\n'))
                        {
                            if (!TryCommitToken(ref token)) return;
                            token.Append(0);
                            TryCreateNewLine();
                        }
                        else
                        {
                            // �ո�ʱ���ж��Ƿ��㹻����ʣ��ռ�
                            if (!EnoughSpace(token.Width + metrics.KernedWidth))
                            {
                                if (!TryCommitToken(ref token)) return;
                            }

                            token.Append(metrics.KernedWidth);
                        }
                    }
                    else
                    {
                        if (!token.TryAdd(metrics.KernedWidth))
                        {
                            if (!TryCommitToken(ref token)) return;
                            token.Append(metrics.KernedWidth);
                        }
                    }
                }
            }
            else
            {
                if (!TryCommitToken(ref token)) return;

                var width = snippet.GetStringLength(font);

                if (!TryAdd(snippet, width))
                    return;
            }
        }

        TryCommitToken(ref token);
    }

    public Vector2 GetStringSize(DynamicSpriteFont font, Vector2 baseScale)
    {
        if (SnippetLines.Count == 0) return new Vector2(0, font.LineSpacing * baseScale.Y);

        var size = Vector2.Zero;

        foreach (var line in SnippetLines)
        {
            size.X = Math.Max(size.X, line.Width);
            size.Y += line.Snippets.Count > 0
                ? font.LineSpacing * line.Snippets.Max(snippet => snippet.Scale)
                : font.LineSpacing;
        }

        return size * baseScale;
    }

    public void DrawText(SpriteBatch spriteBatch, DynamicSpriteFont font,
        Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale,
        out TextSnippet hoveredSnippet, bool ignoreColors = false, bool drawableSpecialSnippet = true)
    {
        hoveredSnippet = null;
        if (baseColor == Color.Transparent) return;

        var currentPosition = position;

        foreach (var line in SnippetLines)
        {
            if (line.Snippets.Count == 0)
            {
                currentPosition.Y += font.LineSpacing;
                continue;
            }

            var maxScale = line.Snippets.Max(l => l.Scale);
            var lineHeight = font.LineSpacing * maxScale * baseScale.Y;

            foreach (var snippet in line.Snippets)
            {
                snippet.Update();

                var snippetColor = ignoreColors
                    ? baseColor
                    : Color.FromNonPremultiplied(snippet.GetVisibleColor().ToVector4() * baseColor.ToVector4());

                var scale = snippet.Scale * baseScale;

                var uniquePosition = currentPosition;
                if (snippet is CursorSnippet cursor)
                {
                    cursor.Font = Font;
                    cursor.TrueHeight = lineHeight;
                }

                if (!snippet.UniqueDraw(!drawableSpecialSnippet, out var snippetSize, spriteBatch, uniquePosition, snippetColor, scale.X))
                {
                    spriteBatch.DrawString(font, snippet.Text, currentPosition, snippetColor, rotation, origin, scale, 0, 0f);
                    snippetSize = font.MeasureString(snippet.Text) * scale;
                }

                if (hoveredSnippet == null)
                {
                    if (new Bounds(currentPosition, snippetSize).Contains(Main.MouseScreen))
                    {
                        hoveredSnippet = snippet;
                    }
                }

                currentPosition.X += _characterSpacing * scale.X + snippetSize.X;
            }

            currentPosition.X = position.X;
            currentPosition.Y += lineHeight;
        }
    }

    public static readonly Vector2[] ShadowOffsets = [-Vector2.UnitX, Vector2.UnitX, -Vector2.UnitY, Vector2.UnitY];

    public void DrawTextShadow(SpriteBatch spriteBatch, DynamicSpriteFont font,
        Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, float spread = 2f)
    {
        var span = ShadowOffsets.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            DrawText(spriteBatch, font,
                position + span[i] * spread, baseColor, rotation, origin, baseScale, out _, ignoreColors: true, false);
        }
    }
}