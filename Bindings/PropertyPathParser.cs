namespace SilkyUIFramework.Bindings;

/// <summary>
/// 属性路径解析器
/// </summary>
public static class PropertyPathParser
{
    /// <summary>
    /// 检查是否为嵌套属性路径
    /// </summary>
    /// <param name="path">属性路径</param>
    /// <returns>是否为嵌套路径</returns>
    public static bool IsNestedPath(string path) =>
        !string.IsNullOrWhiteSpace(path) && path.Contains('.');

    /// <summary>
    /// 解析属性路径
    /// </summary>
    /// <param name="path">属性路径，如 "User.Profile.AvatarUrl"</param>
    /// <returns>属性段列表</returns>
    /// <exception cref="ArgumentException">路径格式无效时抛出</exception>
    public static string[] Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("属性路径不能为空或空白字符串", nameof(path));

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
            throw new ArgumentException("属性路径格式无效", nameof(path));

        // 校验每个段的有效性
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
                throw new ArgumentException($"属性路径段无效：'{segment}'", nameof(path));

            // 预留：后续可扩展索引器语法校验，如 "List[0]", "Dict[\"key\"]" 等
        }

        return segments;
    }
}
