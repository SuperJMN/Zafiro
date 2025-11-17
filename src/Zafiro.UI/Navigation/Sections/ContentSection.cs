using System.Reactive.Linq;
using ReactiveUI.SourceGenerators;

namespace Zafiro.UI.Navigation.Sections;

public partial class ContentSection<T> : ReactiveObject, ISection where T : class
{
    [Reactive] private bool isVisible = true;
    [Reactive] private int sortOrder = 0;

    public ContentSection(string name, IObservable<T> content, object? icon)
    {
        Name = name;
        Icon = icon;
        Content = content.Select(arg => (object)arg);
        RootType = typeof(T);
    }

    public string Name { get; }
    public string FriendlyName => Name;
    public object? Icon { get; }
    public Type RootType { get; }
    public IObservable<object> Content { get; }
}
