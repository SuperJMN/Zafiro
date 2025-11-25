using ReactiveUI.SourceGenerators;
using Zafiro.UI.Navigation;

namespace Zafiro.UI.Navigation.Sections;

public partial class ContentSection<T> : ReactiveObject, ISection, IInitializableSection where T : class
{
    [Reactive] private bool isVisible = true;
    [Reactive] private int sortOrder = 0;
    private readonly Func<INavigator, Task<Result<Unit>>> initialize;

    public ContentSection(string name, IObservable<T> content, object? icon, Func<INavigator, Task<Result<Unit>>> initialize, SectionGroup? group = null)
    {
        this.initialize = initialize;
        Name = name;
        Icon = icon;
        Content = content.Select(arg => (object)arg);
        Group = group;
    }

    public string Name { get; }
    public string FriendlyName => Name;
    public SectionGroup? Group { get; }
    public object? Icon { get; }
    public IObservable<object> Content { get; }

    public Task<Result<Unit>> Initialize(INavigator navigator) => initialize(navigator);
}
