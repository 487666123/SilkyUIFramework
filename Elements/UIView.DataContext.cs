using System.ComponentModel;
using System.Windows.Input;
using SilkyUIFramework.Caches;

namespace SilkyUIFramework.Elements;

public sealed class BindingEntry
{
    public required string SourcePropertyName { get; init; }
    public required string TargetPropertyName { get; init; }
    public Func<object, object> SourcePropertyGetter { private get; set; }
    public required Action<object, object> TargetPropertySetter { private get; init; }

    public void SyncBinding(object source, object target)
    {
        if (SourcePropertyGetter is null) return;
        TargetPropertySetter.Invoke(target, SourcePropertyGetter.Invoke(source));
    }
}

public partial class UIView
{
    public ICommand Command { get; set; }

    protected virtual object CommandParameter => null;

    protected void ExecuteCommand()
    {
        if (Command == null) return;
        var obj = CommandParameter;
        if (Command.CanExecute(obj)) Command.Execute(obj);
    }
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

        var target = ObjectAccessorCache.GetAccessor(GetType());

        var binding = new BindingEntry
        {
            TargetPropertyName = targetPropName,
            SourcePropertyName = sourcePropName,
            TargetPropertySetter = target.GetSetter(targetPropName),
        };
        _bindings[targetPropName] = binding;
        RefreshViewModelSubscription();

        if (DataContext == null) return;
        binding.SourcePropertyGetter = ObjectAccessorCache.GetAccessor(DataContext.GetType()).GetGetter(sourcePropName);
        binding.SyncBinding(DataContext, this);
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

    private void SyncAllBindings()
    {
        var dataContext = DataContext;
        if (dataContext == null) return;

        foreach (var (targetPropName, bindingEntry) in _bindings)
        {
            bindingEntry.SyncBinding(dataContext, this);
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
            if (entry.SourcePropertyName != eventArgs.PropertyName) continue;
            entry.SyncBinding(DataContext, this);
        }
    }

    private void SubscribeViewModel()
    {
        if (DataContext is null) return;
        if (_subscribed) return; _subscribed = true;

        DataContext.PropertyChanged += OnDataContextPropertyChanged;

        var target = ObjectAccessorCache.GetAccessor(DataContext.GetType());

        foreach (var (_, entry) in _bindings)
        {
            entry.SourcePropertyGetter = target.GetGetter(entry.SourcePropertyName);
        }
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
