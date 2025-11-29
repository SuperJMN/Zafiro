using Reactive.Bindings;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

public interface IShell
{
    public object Header { get; set; }
    public ReadOnlyReactiveProperty<object?> ContentHeader { get; }
    IEnumerable<INavigationRoot> Sections { get; }
    global::Reactive.Bindings.ReactiveProperty<INavigationRoot> SelectedSection { get; }
    void GoToSection(string sectionName);
}