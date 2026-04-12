namespace SilkyUIFramework.Bindings;

/// <summary>
/// 绑定项，描述源属性到目标属性的绑定关系
/// </summary>
public sealed class BindingEntry
{
    /// <summary>
    /// 源属性路径数组，支持嵌套属性
    /// </summary>
    public required string[] SourcePropertyPath { get; init; }

    /// <summary>
    /// 目标属性名称
    /// </summary>
    public required string TargetPropertyName { get; init; }

    /// <summary>
    /// 源属性值获取器
    /// </summary>
    public Func<object, object> SourcePropertyGetter { private get; set; }

    /// <summary>
    /// 目标属性值设置器
    /// </summary>
    public required Action<object, object> TargetPropertySetter { private get; init; }

    /// <summary>
    /// 同步绑定值，将源属性值同步到目标属性
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="target">目标对象</param>
    public void SyncBinding(object source, object target)
    {
        if (SourcePropertyGetter is null) return;
        TargetPropertySetter.Invoke(target, SourcePropertyGetter.Invoke(source));
    }

    /// <summary>
    /// 检查是否匹配源属性路径的根属性名称
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <returns>是否匹配</returns>
    public bool MatchSourcePath(string propertyName) => string.Equals(SourcePropertyPath[0], propertyName);
}
