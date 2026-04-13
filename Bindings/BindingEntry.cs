using System.Collections.Immutable;
using System.ComponentModel;
using SilkyUIFramework.Common.Reflection;

namespace SilkyUIFramework.Bindings;

/// <summary>
/// 绑定项，描述源属性到目标属性的绑定关系
/// </summary>
public sealed class BindingEntry
{
    public BindingEntry(string[] sourcePropertyPath, object target, string targetPropertyName)
    {
        SourcePropertyPath = [.. sourcePropertyPath];
        Target = target;
        TargetPropertyName = targetPropertyName;

        _targetPropertySetter = ObjectAccessorCache.GetAccessor(target).GetSetter(targetPropertyName);
    }

    private bool _subscribed = false;

    public object Source
    {
        get; set
        {
            if (ReferenceEquals(field, value)) return;
            Unsubscribe();
            field = value;
            if (field == null) return;
            UpdateSourcePropertyGetter();
            SyncBinding();
            Subscribe();
        }
    }

    /// <summary>
    /// 源属性路径数组，支持嵌套属性
    /// </summary>
    public ImmutableArray<string> SourcePropertyPath { get; }

    public object Target { get; }

    /// <summary>
    /// 目标属性名称
    /// </summary>
    public string TargetPropertyName { get; }

    /// <summary>
    /// 源属性值获取器
    /// </summary>
    private Func<object, object> _sourcePropertyGetter;

    /// <summary>
    /// 目标属性值设置器
    /// </summary>
    private readonly Action<object, object> _targetPropertySetter;

    /// <summary>
    /// 同步绑定值，将源属性值同步到目标属性
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="target">目标对象</param>
    private void SyncBinding()
    {
        if (_sourcePropertyGetter is null) return;
        _targetPropertySetter.Invoke(Target, _sourcePropertyGetter.Invoke(Source));
    }

    private void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (!string.Equals(SourcePropertyPath[0], eventArgs.PropertyName)) return;

        SyncBinding();
    }

    /// <summary>
    /// 更新绑定项的源属性获取器
    /// </summary>
    /// <param name="bindingEntry">绑定项</param>
    /// <param name="source">源对象</param>
    private void UpdateSourcePropertyGetter()
    {
        if (SourcePropertyPath.Length > 1)
        {
            _sourcePropertyGetter = PropertyPathAccessor.Create(Source, [.. SourcePropertyPath]).GetValue;
            return;
        }

        _sourcePropertyGetter = ObjectAccessorCache.GetAccessor(Source).GetGetter(SourcePropertyPath[0]);
    }

    private void Subscribe()
    {
        if (_subscribed) return; _subscribed = true;

        if (Source is not INotifyPropertyChanged notifyPropertyChanged) return;
        notifyPropertyChanged.PropertyChanged += OnSourcePropertyChanged;
    }

    private void Unsubscribe()
    {
        _subscribed = false;

        if (Source is not INotifyPropertyChanged notifyPropertyChanged) return;
        notifyPropertyChanged.PropertyChanged -= OnSourcePropertyChanged;
    }
}
