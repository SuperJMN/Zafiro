using JetBrains.Annotations;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

[PublicAPI]
public class Shell : IShell, IDisposable
{
    public Shell(IEnumerable<ISection> sections, IServiceProvider provider)
    {
        Sections = sections;
        var initial = Sections
            .Where(s => s.IsVisible)
            .OrderBy(s => s.SortOrder)
            .FirstOrDefault();
        SelectedSection = new global::Reactive.Bindings.ReactiveProperty<ISection>(initial);
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

    public void GoToSection(string sectionId)
    {
        SelectedSection.Value = Sections.First(x => x.Id == sectionId);
    }
}