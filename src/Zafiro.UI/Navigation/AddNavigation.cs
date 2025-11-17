using System.Reactive.Concurrency;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public static class AddNavigation
{
    public static IServiceCollection AddNavigator(this IServiceCollection serviceCollection, ILogger? logger = null, IScheduler? scheduler = null)
    {
        serviceCollection.AddScoped<INavigator>(provider => new Navigator(provider, logger.AsMaybe(), scheduler));

        return serviceCollection;
    }
    
    public static IServiceCollection RegisterSections(this IServiceCollection serviceCollection, Action<SectionsBuilder> configure, ILogger? logger = null, IScheduler? scheduler = null)
    {
        serviceCollection.AddScoped<INavigator>(provider => new Navigator(provider, logger.AsMaybe(), scheduler));

        serviceCollection.AddSingleton<IEnumerable<ISection>>(_ =>
        {
            var sectionsBuilder = new SectionsBuilder();
            configure(sectionsBuilder);
            return sectionsBuilder.Build();
        });

        return serviceCollection;
    }
}