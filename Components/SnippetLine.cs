using Terraria.UI.Chat;

namespace SilkyUIFramework.Components;

/// <summary>
/// 记录一行的文本片段和宽度, 不做逻辑处理
/// </summary>
public class SnippetLine()
{
    private readonly List<TextSnippet> _snippets = [];

    public IReadOnlyList<TextSnippet> Snippets => _snippets;

    public float Width { get; set; } = 0f;

    public void Add(TextSnippet snippet, float spacing, float width)
    {
        if (Width > 0)
            Width += spacing + width;
        else
            Width += width;

        _snippets.Add(snippet);
    }

    public void Add(List<TextSnippet> snippets, float spacing, float width)
    {
        if (Width > 0)
            Width += spacing + width;
        else
            Width += width;

        _snippets.AddRange(snippets);
    }
}