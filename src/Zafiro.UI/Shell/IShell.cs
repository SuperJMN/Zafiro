using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

public interface IShell
{
    IEnumerable<ISection> Sections { get; }
    global::Reactive.Bindings.ReactiveProperty<ISection> SelectedSection { get; }
    void GoToSection(string sectionName);
}