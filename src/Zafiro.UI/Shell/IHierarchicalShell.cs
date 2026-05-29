using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

public interface IHierarchicalShell : IShell
{
    SectionLevel RootLevel { get; }
    global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<SectionLevel>> ChildLevels { get; }
    global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<ISection>> SelectedPath { get; }
}
