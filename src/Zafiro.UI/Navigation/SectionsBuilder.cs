using System.Reactive.Linq;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public class SectionsBuilder
{
    private readonly List<ISection> sections = new();

    /// <summary>
    /// Add a section whose initial content is resolved via DI using the view model type <typeparamref name="T"/>.
    /// Callers only provide a name and optional icon.
    /// </summary>
    public SectionsBuilder Add<T>(string name, object? icon = null, SectionGroup? group = null) where T : class
    {
        return Add(name, Observable.Empty<T>(), icon, group);
    }

    /// <summary>
    /// Add a section with an explicit initial content observable.
    /// The type parameter <typeparamref name="T"/> is still used internally as the DI key that
    /// determines the initial navigation target for this section.
    /// </summary>
    public SectionsBuilder Add<T>(string name, IObservable<T> initialContent, object? icon = null, SectionGroup? group = null) where T : class
    {
        sections.Add(new ContentSection<T>(name, initialContent, icon, navigator => navigator.Go(typeof(T)), group));
        return this;
    }

    // Backwards-compat overload: ignore isPrimary
    [Obsolete("Use overload isPrimary is not supported")]
    public SectionsBuilder Add<T>(string name, object? icon, bool isPrimary) where T : class
    {
        return Add<T>(name, icon);
    }

    public IEnumerable<ISection> Build()
    {
        return sections;
    }
}
