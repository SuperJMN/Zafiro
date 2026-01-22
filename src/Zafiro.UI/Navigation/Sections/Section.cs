using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.SourceGenerators;

namespace Zafiro.UI.Navigation.Sections;

public partial class Section : ReactiveObject, ISection
{
    private readonly Lazy<INavigator> navigatorLazy;
    private readonly IServiceScope sectionScope;
    [Reactive] private bool isVisible = true;
    [Reactive] private int sortOrder;

    public Section(string name, IServiceProvider provider, Type initialContentType, object? icon = null, SectionGroup? group = null, string? friendlyName = null)
    {
        Name = name;
        FriendlyName = friendlyName ?? name;
        Group = group ?? new SectionGroup();
        Icon = icon;

        sectionScope = provider.CreateScope();
        navigatorLazy = new Lazy<INavigator>(() =>
        {
            var requiredService = sectionScope.ServiceProvider.GetRequiredService<INavigator>();
            requiredService.SetInitialPage(() => sectionScope.ServiceProvider.GetRequiredService(initialContentType));
            return requiredService;
        });
    }

    public string Name { get; }
    public INavigator Navigator => navigatorLazy.Value;

    public string FriendlyName { get; }

    public SectionGroup Group { get; }

    public object? Icon { get; }

    public void Dispose()
    {
        if (navigatorLazy.IsValueCreated)
        {
            navigatorLazy.Value.Dispose();
        }

        sectionScope.Dispose();
    }
}
