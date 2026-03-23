using Terraria.UI.Chat;

namespace SilkyUIFramework.Components;

/// <summary>
/// 记录一个文本片断和空格, 或者逻辑运算
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