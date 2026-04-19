using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Terraria.UI.Chat;

namespace SilkyUIFramework.Helper;

public static class TextSnippetHelper
{
    public static PlainSnippet Copy(this TextSnippet snippet, string text)
    {
        return new PlainSnippet(text, snippet.Color);
    }

    public static Vector2 GetStringSize(DynamicSpriteFont font, List<TextSnippet> snippets, Vector2 baseScale,
        float maxWidth = -1f)
    {
        // 当前光标（或绘制点）的位置
        var currentPosition = Vector2.Zero;
        // 计算出的文本总尺寸（包围盒）
        var totalSize = Vector2.Zero;
        // 一个基础空格的未缩放宽度
        var baseSpaceWidth = font.MeasureString(" ").X;
        // 当前行所有片段中最大的缩放比例，用于计算行高
        var maxScaleOnCurrentLine = 0.0f;

        // --- 定义一个处理换行的局部函数，以避免代码重复 ---
        void HandleNewLine()
        {
            currentPosition.X = 0f;
            currentPosition.Y += font.LineSpacing * maxScaleOnCurrentLine * baseScale.Y;
            totalSize.Y = Math.Max(totalSize.Y, currentPosition.Y);
            maxScaleOnCurrentLine = 0.0f;
        }

        foreach (var snippet in snippets)
        {
            // --- 处理特殊绘制的片段（例如图标） ---
            if (snippet.UniqueDraw(true, out var uniqueSnippetSize, null, Vector2.Zero, Color.White, baseScale.X))
            {
                currentPosition.X += uniqueSnippetSize.X;
                totalSize.X = Math.Max(totalSize.X, currentPosition.X);
                totalSize.Y = Math.Max(totalSize.Y, currentPosition.Y + uniqueSnippetSize.Y);
            }
            else // --- 处理普通文本片段 ---
            {
                var lines = snippet.Text.Split('\n');
                for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    var words = lines[lineIndex].Split(' ');
                    for (var wordIndex = 0; wordIndex < words.Length; ++wordIndex)
                    {
                        // 在单词之间添加空格的宽度
                        if (wordIndex > 0)
                        {
                            currentPosition.X += baseSpaceWidth * baseScale.X;
                        }

                        // 【优化点1】: 只调用一次 MeasureString
                        var unscaledWordSize = font.MeasureString(words[wordIndex]);
                        var scaledWordWidth = unscaledWordSize.X * baseScale.X;

                        // --- 检查是否需要自动换行 ---
                        if (maxWidth > 0f && currentPosition.X + scaledWordWidth > maxWidth && currentPosition.X > 0f)
                        {
                            // 【优化点2】: 调用提取的局部函数
                            HandleNewLine();
                        }

                        // 累加当前单词的尺寸
                        currentPosition.X += scaledWordWidth;

                        // 更新文本总尺寸
                        totalSize.X = Math.Max(totalSize.X, currentPosition.X);
                        var scaledWordHeight = unscaledWordSize.Y * baseScale.Y;
                        totalSize.Y = Math.Max(totalSize.Y, currentPosition.Y + scaledWordHeight);
                    }

                    // --- 处理显式换行符 '\n' ---
                    var isExplicitNewline = lines.Length > 1 && lineIndex < lines.Length - 1;
                    if (isExplicitNewline)
                    {
                        // 【优化点2】: 调用提取的局部函数
                        HandleNewLine();
                    }
                }
            }
        }

        return totalSize;
    }

    /// <summary> [tag/options:text] </summary>
    public static List<TextSnippet> Parse(this List<TextSnippet> snippets, string input, Color baseColor)
    {
        if (snippets == null) return null;
        snippets.Clear();

        // 删除文本中回车 (怎么会有回车捏?)
        input = input.Replace("\r", "");

        // 创建正则列表
        var matchCollection = ChatManager.Regexes.Format.Matches(input);

        // 文字片段列表
        var inputIndex = 0;

        // 遍历匹配到的正则之间的文本
        foreach (var match in matchCollection.Cast<Match>())
        {
            // match.Index 原字符串中的位置
            // match.Length 长度

            // 如果有, 添加两正则之间的文本.
            if (match.Index > inputIndex)
            {
                snippets.Add(new TextSnippet(input[inputIndex..match.Index], baseColor));
            }

            // 移动下标至当前正则后的第一个字符
            inputIndex = match.Index + match.Length;

            var tag = match.Groups["tag"].Value;
            var text = match.Groups["text"].Value;
            var options = match.Groups["options"].Value;

            if (ChatManager.GetHandler(tag) is { } handler)
            {
                var snippet = handler.Parse(text, baseColor, options);
                snippet.TextOriginal = match.ToString();
                snippets.Add(snippet);
            }
            else snippets.Add(new TextSnippet(text, baseColor));
        }

        if (input.Length > inputIndex)
        {
            snippets.Add(new TextSnippet(input[inputIndex..], baseColor));
        }

        return snippets;
    }

    public static List<TextSnippet> ConvertPlainSnippet(this List<TextSnippet> snippets)
    {
        if (snippets == null) return null;

        var span = CollectionsMarshal.AsSpan(snippets);
        for (var i = 0; i < span.Length; i++)
        {
            var snippet = span[i];

            // 精确判断类型
            if (snippet.GetType() == typeof(TextSnippet))
            {
                span[i] = new PlainSnippet(snippet.Text, snippet.Color);
            }
        }

        return snippets;
    }
}