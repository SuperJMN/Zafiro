using System.Reactive.Linq;
using System.Windows.Input;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public class SectionsBuilder(IServiceProvider provider)
{
    private readonly List<ISection> sections = new();

    private static ISection CreateSection<T>(string name, IServiceProvider provider, object? icon = null, bool isPrimary = true) where T : class
    {
        // Section content is no longer responsible for hosting a SectionScope.
        // We only use the section metadata and its RootType; content is unused here.
        var contentSection = new ContentSection<T>(name, Observable.Empty<T>(), icon)
        {
            IsPrimary = isPrimary,
        };
        return contentSection;
    }

    public SectionsBuilder Add<T>(string name, object? icon = null, bool isPrimary = true) where T : class
    {
        sections.Add(CreateSection<T>(name, provider, icon, isPrimary));
        return this;
    }

    public IEnumerable<ISection> Build()
    {
        return sections;
    }

    public SectionsBuilder Separator(bool isPrimary = true)
    {
        sections.Add(new SectionSeparator
        {
            IsPrimary = isPrimary,
        });
        return this;
    }

    public SectionsBuilder Group(string title, Action<SectionsBuilder> configure, bool isPrimary = true)
    {
        // Add a non-selectable header, then configured items
        sections.Add(new SectionGroupHeader(title)
        {
            IsPrimary = isPrimary,
        });

        var inner = new SectionsBuilder(provider);
        configure(inner);
        foreach (var sec in inner.Build())
        {
            sec.IsPrimary = isPrimary;
            sections.Add(sec);
        }

        return this;
    }

    public SectionsBuilder Command(string name, ICommand command, object? icon, bool isPrimary = true)
    {
        sections.Add(new CommandSection(name, command, icon)
        {
            IsPrimary = isPrimary,
        });
        return this;
    }

    public SectionsBuilder Command(string name, Func<IServiceProvider, ICommand> createCommand, object? icon, bool isPrimary = true)
    {
        sections.Add(new CommandSection(name, createCommand(provider), icon)
        {
            IsPrimary = isPrimary,
        });
        return this;
    }
}
