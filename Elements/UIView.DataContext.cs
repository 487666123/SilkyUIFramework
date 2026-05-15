using System.Windows.Input;
using SilkyUIFramework.Bindings;

namespace SilkyUIFramework.Elements;

/// <summary>
/// UIView 命令相关部分
/// </summary>
public partial class UIView
{
    /// <summary>
    /// 绑定的命令对象，点击时执行
    /// </summary>
    public ICommand Command { get; set; }

    /// <summary>
    /// 命令执行时传递的参数
    /// </summary>
    protected virtual object CommandParameter => null;

    /// <summary>
    /// 执行绑定的命令
    /// </summary>
    protected void ExecuteCommand()
    {
        if (Command == null) return;
        var obj = CommandParameter;
        if (Command.CanExecute(obj)) Command.Execute(obj);
    }
}

/// <summary>
/// UIView 数据上下文相关部分
/// </summary>
public partial class UIView
{
    // 绑定集合，键为目标属性名称
    private readonly Dictionary<string, BindingEntry> _bindings = [];

    #region LocalDataContext & DataContext

    /// <summary>
    /// 本地数据上下文，优先级高于继承自父元素的数据上下文（你不应该频繁变更他）
    /// </summary>
    public object LocalDataContext
    {
        get; set
        {
            if (ReferenceEquals(field, value)) return;
            field = value;
            UpdateDataContext();
        }
    }

    /// <summary>
    /// 生效的数据上下文，优先使用本地设置的，否则继承自父元素
    /// </summary>
    public virtual object DataContext
    {
        get; private set
        {
            if (ReferenceEquals(field, value)) return;
            UnsubscribeDataContext();
            field = value;
            if (IsInsideTree) SubscribeDataContext();
            else UnsubscribeDataContext();
        }
    }

    #endregion

    /// <summary>
    /// 创建源属性到目标属性的绑定
    /// </summary>
    /// <param name="sourcePropName">源属性路径，支持点分隔的嵌套属性</param>
    /// <param name="targetPropName">目标属性名称</param>
    public void Bind(string sourcePropName, string targetPropName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePropName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPropName);

        if (_bindings.TryGetValue(targetPropName, out var binding))
        {
            binding.Source = null;
        }

        binding = _bindings[targetPropName] = new BindingEntry(PropertyPathParser.Parse(sourcePropName), this, targetPropName);

        if (IsInsideTree) binding.Source = DataContext;
    }

    /// <summary>
    /// 订阅数据上下文的属性变更事件
    /// </summary>
    private void SubscribeDataContext()
    {
        if (DataContext == null) return;

        foreach (var (_, value) in _bindings)
        {
            value.Source = DataContext;
        }
    }

    /// <summary>
    /// 取消订阅数据上下文的属性变更事件
    /// </summary>
    private void UnsubscribeDataContext()
    {
        foreach (var (_, value) in _bindings)
        {
            value.Source = null;
        }
    }

    /// <summary>
    /// 更新当前元素的数据上下文，根据本地设置和父元素上下文计算生效的数据上下文
    /// </summary>
    internal virtual void UpdateDataContext()
    {
        DataContext = LocalDataContext ?? Parent?.DataContext;
    }
}
