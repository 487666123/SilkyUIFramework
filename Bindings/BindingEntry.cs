using System.Collections.Immutable;
using System.ComponentModel;
using SilkyUIFramework.Common.Reflection;

namespace SilkyUIFramework.Bindings;

/// <summary>绑定项，描述源属性到目标属性的绑定关系</summary>
public sealed class BindingEntry : IDisposable
{
    /// <summary>订阅节点，保存每一层的属性订阅信息</summary>
    private sealed class SubscriptionNode : IDisposable
    {
        public BindingEntry BindingEntry { get; }
        public INotifyPropertyChanged Object { get; }
        public string PropertyName { get; }
        public int Level { get; }

        public SubscriptionNode(BindingEntry bindingEntry, INotifyPropertyChanged obj, string propertyName, int level)
        {
            BindingEntry = bindingEntry;
            Object = obj;
            PropertyName = propertyName;
            obj.PropertyChanged += Handler;
            Level = level;
        }

        private void Handler(object sender, PropertyChangedEventArgs e)
        {
            var level = Level + 1;

            // 清除后续层级的订阅
            BindingEntry.RemoveSubscriptionsFromLevel(level);

            if (ObjectAccessorCache.GetAccessor(sender).GetGetter(PropertyName).Invoke(sender) is { } obj)
            {
                // 构建后续层级的订阅
                BindingEntry.MountSubscriptionsFromLevel(obj, level);
            }

            // 同步最新值
            BindingEntry.SyncBinding();
        }

        public void Dispose() => Object.PropertyChanged -= Handler;
    }

    /// <summary>创建绑定项</summary>
    /// <param name="sourcePropertyPath">源属性路径</param>
    /// <param name="target">绑定目标对象</param>
    /// <param name="targetPropertyName">目标属性名称</param>
    public BindingEntry(string[] sourcePropertyPath, object target, string targetPropertyName)
    {
        // 参数校验
        ArgumentNullException.ThrowIfNull(sourcePropertyPath);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPropertyName);

        if (sourcePropertyPath.Length == 0)
            throw new ArgumentException("Source property path cannot be empty.", nameof(sourcePropertyPath));
        if (sourcePropertyPath.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Source property path cannot contain null or empty strings.", nameof(sourcePropertyPath));

        SourcePropertyPath = [.. sourcePropertyPath];
        Target = target;
        TargetPropertyName = targetPropertyName;

        _targetPropertySetter = ObjectAccessorCache.GetAccessor(target).GetSetter(targetPropertyName);
    }

    #region Fields Properties

    /// <summary>订阅链</summary>
    private readonly List<SubscriptionNode> _subscriptionChain = [];

    /// <summary>绑定源对象</summary>
    public object Source
    {
        get; set
        {
            if (Equals(field, value)) return;
            RemoveAllSubscriptionNode();
            field = value;
            if (field == null)
            {
                _sourcePropertyGetter = null;
                return;
            }
            UpdateSourcePropertyGetter();
            SyncBinding();
            MountSubscriptionsFromLevel(field, 0);
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

    #endregion

    /// <summary>同步绑定值，将源属性值同步到目标属性</summary>
    private void SyncBinding()
    {
        if (_sourcePropertyGetter is null) return;
        _targetPropertySetter.Invoke(Target, _sourcePropertyGetter.Invoke(Source));
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

    /// <summary>从指定层级开始挂载订阅链</summary>
    private void MountSubscriptionsFromLevel(object obj, int level)
    {
        for (var i = level; i < SourcePropertyPath.Length; i++)
        {
            var propertyName = SourcePropertyPath[i];

            // 如果当前对象实现了INotifyPropertyChanged，订阅它的属性变化
            if (obj is INotifyPropertyChanged notifyPropertyChanged)
            {
                _subscriptionChain.Add(new SubscriptionNode(this, notifyPropertyChanged, propertyName, i));
            }

            if (i == SourcePropertyPath.Length - 1) break;

            var nextLevelGetter = ObjectAccessorCache.GetAccessor(obj).GetGetter(propertyName);
            obj = nextLevelGetter.Invoke(obj);

            if (obj == null) break; // 中间层为null，无法继续
        }
    }

    /// <summary>清理整个订阅链</summary>
    private void RemoveAllSubscriptionNode()
    {
        foreach (var node in _subscriptionChain)
        {
            node.Dispose();
        }

        _subscriptionChain.Clear();
    }

    /// <summary>从指定层级开始移除后面的所有订阅</summary>
    private void RemoveSubscriptionsFromLevel(int level)
    {
        if (level < 0) return;

        // 找到第一个Level >= 指定层级的节点索引
        var startIndex = _subscriptionChain.FindIndex(node => node.Level >= level);
        if (startIndex == -1) return;

        // 从该索引开始移除所有后续节点
        for (var i = startIndex; i < _subscriptionChain.Count; i++)
        {
            _subscriptionChain[i].Dispose();
        }

        _subscriptionChain.RemoveRange(startIndex, _subscriptionChain.Count - startIndex);
    }

    /// <summary>释放所有订阅资源</summary>
    public void Dispose() => RemoveAllSubscriptionNode();
}
