using Terraria.UI.Chat;

namespace SilkyUIFramework.Components;

public sealed class SnippetModule
{
    /// <summary>
    /// 行集合, 一项代表一行
    /// </summary>
    private readonly List<SnippetLine> _lines = [];

    #region 定义字体，最大宽度，最大行数 + 修改方法

    /// <summary>
    /// 获取或设置字体。
    /// </summary>
    private DynamicSpriteFont _font;

    /// <summary>
    /// 获取或设置最大宽度。
    /// </summary>
    private float _maxWidth;

    /// <summary>
    /// 获取或设置最大行数。设置为 0 或负数表示不限制行数。
    /// </summary>
    private int _maxLines;

    public void UpdateProperties(DynamicSpriteFont font, float maxWidth, int maxLines)
    {
        _font = font;
        _maxWidth = maxWidth;
        _maxLines = maxLines;
    }

    #endregion

    /// <summary>
    /// 尝试添加新行
    /// </summary>
    /// <returns>是否添加成功</returns>
    private bool TryAdd()
    {
        if (_maxLines > 0 && _lines.Count >= _maxLines)
            return false;

        _lines.Add(new SnippetLine());
        return true;
    }

    /// <summary>
    /// 判断是否有足够的空间放下接下来的 Snippet
    /// </summary>
    private bool EnoughSpace(float width)
    {
        var line = _lines[^1];
        if (line.Snippets.Count == 0) return true;
        return line.Width + _font.CharacterSpacing + width <= _maxWidth;
    }

    private bool TryAdd(TextSnippet snippet, float width)
    {
        var current = _lines[^1];
        if (current.Snippets.Count == 0)
        {
            current.Add(snippet, _font.CharacterSpacing, width);
            return true;
        }

        if (current.Width + _font.CharacterSpacing + width > _maxWidth)
        {
            if (!TryAdd())
                return false;
            current = _lines[^1];
        }

        current.Add(snippet, _font.CharacterSpacing, width);
        return true;
    }

    private bool TryCommitToken(ref SnippetToken token)
    {
        if (token.Word == 0) return true;

        var width = token.Width;
        var snippet = token.ExtractSnippets();

        var current = _lines[^1];
        if (current.Snippets.Count == 0)
        {
            current.Add(snippet, _font.CharacterSpacing, width);
            return true;
        }

        if (current.Width + _font.CharacterSpacing + width > _maxWidth)
        {
            if (!TryAdd())
                return false;
            current = _lines[^1];
        }

        current.Add(snippet, _font.CharacterSpacing, width);
        return true;
    }

    public void FromSnippets(List<TextSnippet> snippets)
    {
        _lines.Clear();
        TryAdd();
        if (_font == null) return;

        var spacing = _font.CharacterSpacing;

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
                            _lines[^1].Add(snippet.Copy(text[last..i]), spacing, width);
                        if (!TryAdd()) return;
                        width = 0f;
                        last = i + 1;
                    }
                    else
                    {
                        if (width > 0) width += spacing;
                        width += _font.GetCharacterMetrics(c).KernedWidth;
                    }
                }

                if (last < text.Length)
                {
                    var part = text[last..];
                    _lines[^1].Add(snippet.Copy(part), spacing, width);
                }
            }
            else
            {
                snippet.UniqueDraw(true, out var size, null);
                _lines[^1].Add(snippet, spacing, size.X);
            }
        }
    }

    /// <summary>
    /// 单词换行 (单词不限宽!)
    /// </summary>
    public void WordWrapSnippets(List<TextSnippet> snippets)
    {
        _lines.Clear();

        if (_font == null) return;

        TryAdd();

        if (snippets is null || snippets.Count == 0) return;

        var font = _font;
        var token = new SnippetToken(_maxWidth, font.CharacterSpacing);

        // 循环中有 Snippet 三种情况
        // 1. CursorSnippet 光标，SilkyUI 中最特殊的 Snippet
        // 2. PlainSnippet  最正常普通的文本，可拆分
        // 3. CustomSnippet 自定义的 TextSnippet 意味着不可拆分

        foreach (var snippet in snippets)
        {
            // 光标片段
            if (snippet is CursorSnippet)
            {
                token.Snippets.Add(snippet);
                token.Add(0, false);
            }
            else if (snippet is PlainSnippet plainSnippet)
            {
                token.Snippets.Add(plainSnippet);

                foreach (var c in snippet.Text)
                {
                    var metrics = font.GetCharacterMetrics(c);

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
                            token.Add(0);
                            TryAdd();
                        }
                        else
                        {
                            if (!EnoughSpace(token.Width + metrics.KernedWidth))
                            {
                                if (!TryCommitToken(ref token)) return;
                            }

                            token.Add(metrics.KernedWidth);
                        }
                    }
                    else
                    {
                        if (!token.TryAdd(metrics.KernedWidth))
                        {
                            if (!TryCommitToken(ref token)) return;
                            token.Add(metrics.KernedWidth);
                        }
                    }
                }
            }
            else
            {
                if (!TryCommitToken(ref token)) return;

                snippet.UniqueDraw(true, out var size, null);

                if (!TryAdd(snippet, size.X))
                    return;
            }
        }

        TryCommitToken(ref token);
    }

    public Vector2 GetStringSize(DynamicSpriteFont font, Vector2 baseScale)
    {
        if (_lines.Count == 0) return new Vector2(0, font.LineSpacing * baseScale.Y);

        var size = Vector2.Zero;

        foreach (var line in _lines)
        {
            size.X = Math.Max(size.X, line.Width);
            size.Y += font.LineSpacing;
        }

        return size * baseScale;
    }

    #region Draw

    public void DrawText(SpriteBatch spriteBatch, DynamicSpriteFont font,
        Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale,
        out TextSnippet hoveredSnippet, bool ignoreColors = false, bool drawableSpecialSnippet = true)
    {
        hoveredSnippet = null;
        if (baseColor == Color.Transparent) return;

        var currentPosition = position;

        foreach (var line in _lines)
        {
            if (line.Snippets.Count == 0)
            {
                currentPosition.Y += font.LineSpacing;
                continue;
            }

            const float maxScale = 1;
            var lineHeight = font.LineSpacing * maxScale * baseScale.Y;

            foreach (var snippet in line.Snippets)
            {
                var snippetColor = ignoreColors
                    ? baseColor
                    : Color.FromNonPremultiplied(snippet.GetVisibleColor().ToVector4() * baseColor.ToVector4());

                var scale = baseScale;

                var uniquePosition = currentPosition;
                if (snippet is CursorSnippet cursor)
                {
                    cursor.Font = _font;
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

                currentPosition.X += _font.CharacterSpacing * scale.X + snippetSize.X;
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

    #endregion
}