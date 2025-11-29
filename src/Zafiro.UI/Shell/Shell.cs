using System.Reactive.Disposables;
using JetBrains.Annotations;
using Reactive.Bindings;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

[PublicAPI]
public class Shell : IShell, IDisposable
{
    private readonly CompositeDisposable disposables = new();

    public Shell(ShellProperties shellProperties, IEnumerable<ISection> sections, IServiceProvider provider)
    {
        Sections = sections;
        SelectedSection = new global::Reactive.Bindings.ReactiveProperty<ISection>(Sections.FirstOrDefault());
    }

    public void Dispose()
    {
        foreach (var section in Sections)
        {
            section.Dispose();
        }

        disposables.Dispose();
    }

    public object Header { get; set; }
    public ReadOnlyReactiveProperty<object?> ContentHeader { get; }
    public IEnumerable<ISection> Sections { get; }
    public global::Reactive.Bindings.ReactiveProperty<ISection> SelectedSection { get; }

    public void GoToSection(string sectionName)
    {
        SelectedSection.Value = Sections.First(x => x.Name == sectionName);
    }
}