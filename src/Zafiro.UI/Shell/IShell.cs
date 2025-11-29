using Reactive.Bindings;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

public interface IShell
{
    public object Header { get; set; }
    public ReadOnlyReactiveProperty<object?> ContentHeader { get; }
    IEnumerable<ISection> Sections { get; }
    global::Reactive.Bindings.ReactiveProperty<ISection> SelectedSection { get; }
    void GoToSection(string sectionName);
}