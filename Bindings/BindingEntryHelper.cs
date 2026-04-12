using SilkyUIFramework.Common.Reflection;

namespace SilkyUIFramework.Bindings;

/// <summary>
/// 绑定辅助类，提供绑定相关的扩展方法
/// </summary>
public static class BindingEntryHelper
{
    /// <summary>
    /// 更新绑定项的源属性获取器
    /// </summary>
    /// <param name="bindingEntry">绑定项</param>
    /// <param name="source">源对象</param>
    public static void UpdateSourcePropertyGetter(this BindingEntry bindingEntry, object source)
    {
        if (bindingEntry.SourcePropertyPath.Length > 1)
        {
            bindingEntry.SourcePropertyGetter =
                PropertyPathAccessor.Create(source.GetType(), bindingEntry.SourcePropertyPath).GetValue;
        }
        else
        {
            bindingEntry.SourcePropertyGetter =
                ObjectAccessorCache.GetAccessor(source.GetType()).GetGetter(bindingEntry.SourcePropertyPath[0]);
        }
    }
}
