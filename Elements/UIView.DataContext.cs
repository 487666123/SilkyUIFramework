using System.ComponentModel;
using SilkyUIFramework.Caches;

namespace SilkyUIFramework.Elements;

public sealed class BindingEntry
{
    public required string SourcePropertyName { get; init; }
    public required string TargetPropertyName { get; init; }
}

public partial class UIView
{
    private bool _subscribed = false;

    public INotifyPropertyChanged LocalDataContext
    {
        get; set
        {
            if (ReferenceEquals(field, value)) return;
            field = value;
            RefreshDataContext();
        }
    }

    public virtual INotifyPropertyChanged DataContext
    {
        get; private set
        {
            if (ReferenceEquals(field, value)) return;
            UnsubscribeViewModel();
            field = value;
            RefreshViewModelSubscription();
        }
    }

    private readonly Dictionary<string, BindingEntry> _bindings = [];

    public void Bind(string sourcePropName, string targetPropName)
    {
        ValidateBinding(sourcePropName, targetPropName);

        var binding = new BindingEntry { TargetPropertyName = targetPropName, SourcePropertyName = sourcePropName };
        _bindings[targetPropName] = binding;
        SyncBinding(targetPropName);
        RefreshViewModelSubscription();
    }

    private bool ShouldSubscribeViewModel => IsInsideTree && _bindings.Count > 0;

    private void RefreshViewModelSubscription()
    {
        if (ShouldSubscribeViewModel) SubscribeViewModel();
        else UnsubscribeViewModel();
    }

    private void ValidateBinding(string sourcePropName, string targetPropName)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePropName);
        ArgumentException.ThrowIfNullOrEmpty(targetPropName);

        var target = ObjectAccessorCache.GetAccessor(GetType());
        target.GetSetter(targetPropName);

        var dataContext = DataContext;
        if (dataContext == null) return;

        var source = ObjectAccessorCache.GetAccessor(dataContext.GetType());
        source.GetGetter(sourcePropName);
    }

    private void SyncBinding(string targetPropName)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetPropName);

        var dataContext = DataContext;
        if (dataContext == null) return;
        if (!_bindings.TryGetValue(targetPropName, out var bindingEntry)) return;

        var source = ObjectAccessorCache.GetAccessor(dataContext.GetType());
        var target = ObjectAccessorCache.GetAccessor(GetType());

        target[this, targetPropName] = source[dataContext, bindingEntry.SourcePropertyName];
    }

    private void SyncAllBindings()
    {
        var dataContext = DataContext;
        if (dataContext == null) return;

        var source = ObjectAccessorCache.GetAccessor(dataContext.GetType());
        var target = ObjectAccessorCache.GetAccessor(GetType());

        foreach (var (targetPropName, bindingEntry) in _bindings)
        {
            target[this, targetPropName] = source[dataContext, bindingEntry.SourcePropertyName];
        }
    }

    protected virtual void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
    {
        if (string.IsNullOrEmpty(eventArgs.PropertyName))
        {
            SyncAllBindings(); return;
        }

        foreach (var (_, entry) in _bindings)
        {
            if (!entry.SourcePropertyName.Equals(eventArgs.PropertyName)) continue;

            var source = ObjectAccessorCache.GetAccessor(sender.GetType());
            var target = ObjectAccessorCache.GetAccessor(GetType());

            target[this, entry.TargetPropertyName] = source[sender, entry.SourcePropertyName];
        }
    }

    private void SubscribeViewModel()
    {
        if (_subscribed) return;
        _subscribed = true;
        DataContext?.PropertyChanged += OnDataContextPropertyChanged;
    }

    private void UnsubscribeViewModel()
    {
        _subscribed = false;
        DataContext?.PropertyChanged -= OnDataContextPropertyChanged;
    }

    internal virtual void RefreshDataContext()
    {
        var effectiveDataContext = LocalDataContext ?? Parent?.DataContext;

        if (ReferenceEquals(DataContext, effectiveDataContext)) return;

        DataContext = effectiveDataContext;
        SyncAllBindings();
    }
}
