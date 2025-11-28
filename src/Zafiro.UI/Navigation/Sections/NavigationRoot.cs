using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.SourceGenerators;

namespace Zafiro.UI.Navigation.Sections;

public partial class NavigationRoot<TInitial> : ReactiveObject, INavigationRoot, IDisposable where TInitial : class
{
    private readonly SectionGroup group;
    private readonly Lazy<INavigator> navigator;
    private readonly Lazy<IServiceScope> scope;
    [Reactive] private bool isVisible = true;
    [Reactive] private int sortOrder;

    public NavigationRoot(string name, IServiceProvider provider, object? icon = null, SectionGroup? group = null, string? friendlyName = null)
    {
        Name = name;
        FriendlyName = friendlyName ?? name;
        Icon = icon;
        this.group = group ?? new SectionGroup();

        scope = new Lazy<IServiceScope>(provider.CreateScope);
        navigator = new Lazy<INavigator>(() =>
        {
            var sp = scope.Value.ServiceProvider;
            var logger = Maybe<ILogger>.From(sp.GetService<ILogger>());
            var scheduler = sp.GetService<IScheduler>();
            return new Navigator<TInitial>(sp, logger, scheduler);
        });
    }

    public void Dispose()
    {
        if (scope.IsValueCreated)
        {
            scope.Value.Dispose();
        }
    }

    public string Name { get; }

    public string FriendlyName { get; }

    public SectionGroup Group => group;

    public object? Icon { get; }

    public INavigator Navigator => navigator.Value;
}