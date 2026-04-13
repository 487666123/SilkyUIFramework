using System.Collections.Immutable;
using System.ComponentModel;
using SilkyUIFramework.Common.Reflection;

namespace SilkyUIFramework.Bindings;

/// <summary>绑定项，描述源属性到目标属性的绑定关系</summary>
public sealed class BindingEntry
{
    /// <summary>创建绑定项</summary>
    /// <param name="sourcePropertyPath">源属性路径</param>
    /// <param name="target">绑定目标对象</param>
    /// <param name="targetPropertyName">目标属性名称</param>
    public BindingEntry(string[] sourcePropertyPath, object target, string targetPropertyName)
    {
        SourcePropertyPath = [.. sourcePropertyPath];
        Target = target;
        TargetPropertyName = targetPropertyName;

        _targetPropertySetter = ObjectAccessorCache.GetAccessor(target).GetSetter(targetPropertyName);
    }

    /// <summary>是否已订阅属性变化通知</summary>
    private bool _subscribed = false;

    /// <summary>绑定源对象</summary>
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

    /// <summary>源属性路径数组，支持嵌套属性</summary>
    public ImmutableArray<string> SourcePropertyPath { get; }

    /// <summary>目标</summary>
    public object Target { get; }

    /// <summary>目标属性名称</summary>
    public string TargetPropertyName { get; }

    /// <summary>源属性值获取器</summary>
    private Func<object, object> _sourcePropertyGetter;

    /// <summary>目标属性值设置器</summary>
    private readonly Action<object, object> _targetPropertySetter;

    /// <summary>同步绑定值，将源属性值同步到目标属性</summary>
    private void SyncBinding()
    {
        if (_sourcePropertyGetter is null) return;
        _targetPropertySetter.Invoke(Target, _sourcePropertyGetter.Invoke(Source));
    }

    /// <summary>源属性变化事件处理方法</summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="eventArgs">属性变化事件参数</param>
    private void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (!string.Equals(SourcePropertyPath[0], eventArgs.PropertyName)) return;

        SyncBinding();
    }

    /// <summary>更新绑定项的源属性获取器</summary>
    private void UpdateSourcePropertyGetter()
    {
        if (SourcePropertyPath.Length > 1)
        {
            _sourcePropertyGetter = PropertyPathAccessor.Create(Source, [.. SourcePropertyPath]).GetValue;
            return;
        }

        _sourcePropertyGetter = ObjectAccessorCache.GetAccessor(Source).GetGetter(SourcePropertyPath[0]);
    }

    /// <summary>订阅源对象的属性变化通知</summary>
    private void Subscribe()
    {
        if (_subscribed) return; _subscribed = true;

        if (Source is not INotifyPropertyChanged notifyPropertyChanged) return;
        notifyPropertyChanged.PropertyChanged += OnSourcePropertyChanged;
    }

    /// <summary>取消订阅源对象的属性变化通知</summary>
    private void Unsubscribe()
    {
        _subscribed = false;

        if (Source is not INotifyPropertyChanged notifyPropertyChanged) return;
        notifyPropertyChanged.PropertyChanged -= OnSourcePropertyChanged;
    }
}
