using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.SourceGenerators;

namespace Zafiro.UI.Navigation.Sections;

public partial class Section : ReactiveObject, ISection
{
    private readonly Lazy<INavigator> navigatorLazy;
    [Reactive] private bool isVisible = true;
    [Reactive] private int sortOrder;

    public Section(string name, IServiceProvider provider, Type initialContentType, object? icon = null, SectionGroup? group = null, string? friendlyName = null)
    {
        Name = name;
        FriendlyName = friendlyName ?? name;
        Group = group ?? new SectionGroup();
        Icon = icon;

        var scope = provider.CreateScope();
        navigatorLazy = new Lazy<INavigator>(() =>
        {
            var requiredService = scope.ServiceProvider.GetRequiredService<INavigator>();
            requiredService.Go(initialContentType);
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
        Navigator.Dispose();
    }
}