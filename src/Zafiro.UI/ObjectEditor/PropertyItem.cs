using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reflection;
using Zafiro.Values;

namespace Zafiro.UI.ObjectEditor;

public abstract class PropertyItem<T> : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly GroupGetter groupGetter;
    private readonly GroupSetter groupSetter;
    private readonly PropertyInfo propertyInfo;
    private readonly IEnumerable<object> targets;

    public PropertyItem(T valueEditor, PropertyInfo propertyInfo, IEnumerable<object> targets)
    {
        this.propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        this.targets = targets ?? throw new ArgumentNullException(nameof(targets));

        ValueEditor = valueEditor;
        groupSetter = new GroupSetter(propertyInfo);
        groupGetter = new GroupGetter(propertyInfo);
        SubscribeToPropertyChangesOf(targets.OfType<INotifyPropertyChanged>());
    }


    public string PropertyName => propertyInfo.Name;

    public T ValueEditor { get; }

    public object Value
    {
        get => groupGetter.GetValue(targets);
        set
        {
            groupSetter.Set(targets, value);
            this.RaisePropertyChanged();
        }
    }

    public void Dispose()
    {
        disposables.Dispose();
    }

    private void SubscribeToPropertyChangesOf(IEnumerable<INotifyPropertyChanged> observables)
    {
        var subscriptions = from observable in observables
            let subscription =
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                        h => observable.PropertyChanged += h, h => PropertyChanged -= h)
                    .Subscribe(args => OnTargetPropertyChanged(args.EventArgs.PropertyName))
            select subscription;

        foreach (var subscription in subscriptions) disposables.Add(subscription);
    }

    private void OnTargetPropertyChanged(string propertyName)
    {
        if (string.Equals(PropertyName, propertyName, StringComparison.Ordinal))
        {
            this.RaisePropertyChanged(nameof(Value));
        }
    }
}