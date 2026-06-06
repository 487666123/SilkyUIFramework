using Terraria.UI.Chat;

namespace SilkyUIFramework.Components;

public ref struct SnippetToken(float maxWidth, float spacing)
{
    public readonly float MaxWidth { get; } = maxWidth;
    public readonly float CharacterSpacing { get; } = spacing;

    public List<TextSnippet> Snippets { get; } = [];

    public bool IsWhiteSpace { get; set; } = false;
    public int Word { get; private set; } = 0;
    public float Width { get; private set; } = -spacing;

    public void Add(float width, bool usingGap = true)
    {
        Word++;
        // 首次添加不计算间隔
        if (Width == 0) Width = width;
        else
        {
            if (usingGap) Width += CharacterSpacing + width;
            else Width += width;
        }
    }

    /// <summary>
    /// 尝试添加一个字符到当前行。<br/>
    /// 如果当前行什么都没有，则直接添加。<br/>
    /// 如果当前行已经有内容，则检查添加该字符后是否会超出最大宽度，如果不会，则添加；否则，返回 false。<br/>
    /// 最大宽度是字符串的最大像素长度，而不是字符数。也就是说
    /// </summary>
    public bool TryAdd(float width)
    {
        // 首次添加不计算间隔
        if (Width == 0)
        {
            Word++;
            Width = width;
            return true;
        }

        if (Width + CharacterSpacing + width > MaxWidth) return false;

        Word++;
        Width += CharacterSpacing + width;
        return true;
    }

    public List<TextSnippet> ExtractSnippets()
    {
        if (Snippets.Count == 0) return [];

        var count = Word;

        var cutSnippets = new List<TextSnippet>();

        var span = Snippets.ToArray().AsSpan();
        Snippets.Clear();

        foreach (var snippet in span)
        {
            // 完全包裹 & 结束
            // 至少算一个
            var snippetLength = Math.Max(1, snippet.Text.Length);
            if (count >= snippetLength)
            {
                cutSnippets.Add(snippet);
                count -= snippetLength;
                continue;
            }

            cutSnippets.Add(snippet.Copy(snippet.Text[..count]));
            Snippets.Add(snippet.Copy(snippet.Text[count..]));
            break;
        }

        Word = 0;
        Width = 0f;

        return cutSnippets;
    }
}