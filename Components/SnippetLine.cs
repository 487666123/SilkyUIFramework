using Terraria.UI.Chat;

namespace SilkyUIFramework.Components;

public class SnippetLine()
{
    private readonly List<TextSnippet> _snippets = [];
    public int Count => _snippets.Count;

    public IReadOnlyList<TextSnippet> Snippets => _snippets;

    public float Width { get; set; } = 0f;

    public void Append(TextSnippet snippet, float spacing, float width)
    {
        if (Width > 0)
            Width += spacing;

        Width += width;
        _snippets.Add(snippet);
    }

    public void Append(List<TextSnippet> snippet, float spacing, float width)
    {
        if (Width > 0)
            Width += spacing;

        Width += width;
        _snippets.AddRange(snippet);
    }
}