using System.Reactive.Concurrency;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public class SectionsBuilder
{
    private readonly List<Func<IServiceProvider, IScheduler?, Maybe<ILogger>, ISection>> sectionFactories = new();

    /// <summary>
    ///     Add a section providing both the key name and a friendly name.
    /// </summary>
    public SectionsBuilder AddSection<T>(string name, string friendlyName, object? icon = null, SectionGroup? group = null, int sortOrder = 0, string? shortName = null) where T : class
    {
        return AddSection<T>(name, friendlyName, icon, group, sortOrder, shortName, null);
    }

    public SectionsBuilder AddSection<T>(string name, string friendlyName, object? icon, SectionGroup? group, int sortOrder, string? shortName, string? parentId) where T : class
    {
        sectionFactories.Add((provider, scheduler, logger) =>
        {
            var root = new Section(name, provider, typeof(T), icon, group, friendlyName, parentId)
            {
                SortOrder = sortOrder,
                ShortName = shortName
            };
            return root;
        });
        return this;
    }

    public IEnumerable<ISection> Build(IServiceProvider provider, IScheduler? scheduler = null, Maybe<ILogger> logger = default)
    {
        return sectionFactories.Select(factory => factory(provider, scheduler, logger)).ToList();
    }
}
