using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public class SectionsBuilder
{
    private readonly List<Func<IServiceProvider, INavigationRoot>> sectionFactories = new();

    /// <summary>
    /// Add a section whose initial content is resolved via DI using the view model type <typeparamref name="T"/>.
    /// Callers only provide a name and optional icon.
    /// </summary>
    public SectionsBuilder Add<T>(string name, object? icon = null, SectionGroup? group = null) where T : class
    {
        return AddSection<T>(name, name, icon, group);
    }

    /// <summary>
    /// Add a section providing both the key name and a friendly name.
    /// </summary>
    public SectionsBuilder AddSection<T>(string name, string friendlyName, object? icon = null, SectionGroup? group = null, int sortOrder = 0) where T : class
    {
        sectionFactories.Add(provider =>
        {
            var root = new NavigationRoot<T>(name, provider, icon, group, friendlyName)
            {
                SortOrder = sortOrder
            };
            return root;
        });
        return this;
    }

    /// <summary>
    /// Add a section with an explicit initial content observable.
    /// The type parameter <typeparamref name="T"/> is still used internally as the DI key that
    /// determines the initial navigation target for this section.
    /// </summary>
    public SectionsBuilder Add<T>(string name, IObservable<T> initialContent, object? icon = null, SectionGroup? group = null) where T : class
    {
        sectionFactories.Add(provider =>
        {
            var root = new NavigationRoot<T>(name, provider, initialContent, icon, group, name);
            return root;
        });

        return this;
    }

    // Backwards-compat overload: ignore isPrimary
    [Obsolete("Use overload isPrimary is not supported")]
    public SectionsBuilder Add<T>(string name, object? icon, bool isPrimary) where T : class
    {
        return Add<T>(name, icon);
    }

    public IEnumerable<INavigationRoot> Build(IServiceProvider provider)
    {
        return sectionFactories.Select(factory => factory(provider)).ToList();
    }
}