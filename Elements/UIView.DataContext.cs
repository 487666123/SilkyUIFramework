using System.ComponentModel;
using System.Windows.Input;
using SilkyUIFramework.Bindings;
using SilkyUIFramework.Common.Reflection;

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
    // 是否需要订阅视图模型变更事件
    private bool ShouldSubscribeViewModel => IsInsideTree && _bindings.Count > 0;

    // 是否已订阅数据上下文变更事件
    private bool _subscribed = false;
    // 绑定集合，键为目标属性名称
    private readonly Dictionary<string, BindingEntry> _bindings = [];

    /// <summary>
    /// 本地数据上下文，优先级高于继承自父元素的数据上下文
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

            // 必须在 UI 树中订阅
            // 如果此时未订阅，则在进入树时会订阅
            if (ShouldSubscribeViewModel) SubscribeDataContext();
            else UnsubscribeDataContext();
        }
    }

    /// <summary>
    /// 创建源属性到目标属性的绑定
    /// </summary>
    /// <param name="sourcePropName">源属性路径，支持点分隔的嵌套属性</param>
    /// <param name="targetPropName">目标属性名称</param>
    public void Bind(string sourcePropName, string targetPropName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePropName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPropName);

        var bindingEntry = _bindings[targetPropName] = new BindingEntry
        {
            TargetPropertyName = targetPropName,
            SourcePropertyPath = PropertyPathParser.Parse(sourcePropName),
            TargetPropertySetter = ObjectAccessorCache.GetAccessor(GetType()).GetSetter(targetPropName),
        };

        if (ShouldSubscribeViewModel) SubscribeDataContext();

        if (DataContext == null) return;

        bindingEntry.UpdateSourcePropertyGetter(DataContext);
        bindingEntry.SyncBinding(DataContext, this);
    }

    /// <summary>
    /// 同步所有绑定的值
    /// </summary>
    private void SyncAllBindings()
    {
        var dataContext = DataContext;
        if (dataContext == null) return;

        foreach (var (_, bindingEntry) in _bindings)
        {
            bindingEntry.SyncBinding(dataContext, this);
        }
    }

    /// <summary>
    /// 订阅数据上下文的属性变更事件
    /// </summary>
    private void SubscribeDataContext()
    {
        if (DataContext is null) return;
        if (_subscribed) return; _subscribed = true;

        if (DataContext is INotifyPropertyChanged notifyPropertyChanged)
            notifyPropertyChanged.PropertyChanged += OnDataContextPropertyChanged;

        UpdateBindingsSourcePropertyGetter(DataContext);
    }

    /// <summary>
    /// 取消订阅数据上下文的属性变更事件
    /// </summary>
    private void UnsubscribeDataContext()
    {
        _subscribed = false;

        if (DataContext is INotifyPropertyChanged notifyPropertyChanged)
            notifyPropertyChanged.PropertyChanged -= OnDataContextPropertyChanged;
    }

    /// <summary>
    /// 数据上下文属性变更时的处理方法
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="eventArgs">属性变更事件参数</param>
    protected virtual void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(eventArgs.PropertyName)) return;

        foreach (var (_, bindingEntry) in _bindings)
        {
            if (!bindingEntry.MatchSourcePath(eventArgs.PropertyName)) continue;
            bindingEntry.SyncBinding(DataContext, this);
        }
    }

    /// <summary>
    /// 更新当前元素的数据上下文，根据本地设置和父元素上下文计算生效的数据上下文
    /// </summary>
    internal virtual void UpdateDataContext()
    {
        var dataContext = LocalDataContext ?? Parent?.DataContext;

        if (ReferenceEquals(DataContext, dataContext)) return;
        DataContext = dataContext;

        UpdateBindingsSourcePropertyGetter(dataContext);
    }

    /// <summary> 更新绑定员属性 getter </summary>
    private void UpdateBindingsSourcePropertyGetter(object dataContext)
    {
        if (dataContext == null) return;

        foreach (var (_, bindingEntry) in _bindings)
        {
            bindingEntry.UpdateSourcePropertyGetter(dataContext);
        }

        SyncAllBindings();
    }
}
