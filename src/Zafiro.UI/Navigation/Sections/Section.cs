using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.SourceGenerators;

namespace Zafiro.UI.Navigation.Sections;

public partial class Section<TInitial> : ReactiveObject, ISection where TInitial : class
{
    private readonly Lazy<INavigator> navigator;
    private readonly Lazy<IServiceScope> scope;
    [Reactive] private bool isVisible = true;
    [Reactive] private int sortOrder;

    public Section(string name, IServiceProvider provider, IObservable<TInitial> initialContent, IScheduler? scheduler, Maybe<ILogger> logger, object? icon = null, SectionGroup? group = null, string? friendlyName = null)
    {
        Name = name;
        FriendlyName = friendlyName ?? name;
        Icon = icon;
        this.Group = group ?? new SectionGroup();

        scope = new Lazy<IServiceScope>(provider.CreateScope);
        navigator = new Lazy<INavigator>(() =>
        {
            var sp = scope.Value.ServiceProvider;
            return new Navigator(sp, logger, scheduler, initialContent.Select(x => (object)x));
        });
    }

    public void Dispose()
    {
        if (navigator.IsValueCreated && navigator.Value is IDisposable disposableNavigator)
        {
            disposableNavigator.Dispose();
        }

        if (scope.IsValueCreated)
        {
            scope.Value.Dispose();
        }
    }

    public string Name { get; }

    public string FriendlyName { get; }

    public SectionGroup Group { get; }

    public object? Icon { get; }

    public INavigator Navigator => navigator.Value;
}