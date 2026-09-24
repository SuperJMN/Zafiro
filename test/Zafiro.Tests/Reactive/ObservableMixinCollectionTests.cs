using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Zafiro.Reactive;

namespace Zafiro.Tests.Reactive;

public class ObservableMixinCollectionTests
{
    [Fact]
    public void Collection_binding_accepts_plain_property_change_notifications()
    {
        var owner = new CollectionOwner();

        using var subscription = owner.UpdateCollectionWhenSomeOtherCollectionObservableChanges(
            value => value.Items,
            out var boundItems);

        Assert.Contains(1, boundItems);

        owner.Items = CreateReadOnlyCollection(2, 3);

        Assert.Contains(2, boundItems);
        Assert.Contains(3, boundItems);
    }

    private static ReadOnlyObservableCollection<int> CreateReadOnlyCollection(params int[] items)
    {
        return new ReadOnlyObservableCollection<int>(new ObservableCollection<int>(items));
    }

    private sealed class CollectionOwner : INotifyPropertyChanged
    {
        private ReadOnlyObservableCollection<int> items = CreateReadOnlyCollection(1);

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReadOnlyObservableCollection<int> Items
        {
            get => items;
            set
            {
                items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            }
        }
    }
}
