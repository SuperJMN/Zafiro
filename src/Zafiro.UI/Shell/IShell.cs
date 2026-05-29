using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

public interface IShell
{
    IEnumerable<ISection> Sections { get; }
    SectionLevel RootLevel { get; }
    global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<SectionLevel>> ChildLevels { get; }
    global::Reactive.Bindings.ReactiveProperty<ISection> SelectedSection { get; }
    global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<ISection>> SelectedPath { get; }
    void GoToSection(string sectionId);
}
