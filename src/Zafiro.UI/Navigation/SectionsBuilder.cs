using System.Reactive.Linq;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public class SectionsBuilder(IServiceProvider provider)
{
    private readonly List<ISection> sections = new();

    private static ISection CreateSection<T>(string name, IServiceProvider provider, object? icon = null) where T : class
    {
        // Section content is no longer responsible for hosting a SectionScope.
        // We only use the section metadata and its RootType; content is unused here.
        var contentSection = new ContentSection<T>(name, Observable.Empty<T>(), icon);
        return contentSection;
    }

    public SectionsBuilder Add<T>(string name, object? icon = null) where T : class
    {
        sections.Add(CreateSection<T>(name, provider, icon));
        return this;
    }

    // Backwards-compat overload: ignore isPrimary
    public SectionsBuilder Add<T>(string name, object? icon, bool isPrimary) where T : class
    {
        return Add<T>(name, icon);
    }

    public IEnumerable<ISection> Build()
    {
        return sections;
    }

    public SectionsBuilder Separator()
    {
        sections.Add(new SectionSeparator());
        return this;
    }

    // Backwards-compat overload: ignore isPrimary
    public SectionsBuilder Separator(bool isPrimary)
    {
        return Separator();
    }
}