using JetBrains.Annotations;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

[PublicAPI]
public class Shell : IShell, IDisposable
{
    public Shell(IEnumerable<ISection> sections, IServiceProvider provider)
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
    }

    public IEnumerable<ISection> Sections { get; }
    public global::Reactive.Bindings.ReactiveProperty<ISection> SelectedSection { get; }

    public void GoToSection(string sectionName)
    {
        SelectedSection.Value = Sections.First(x => x.Name == sectionName);
    }
}