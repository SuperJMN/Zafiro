using System.Reactive.Concurrency;
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

    public static IServiceCollection RegisterNavigationRoots(this IServiceCollection serviceCollection, Func<IServiceProvider, IEnumerable<INavigationRoot>> factory, ILogger? logger = null, IScheduler? scheduler = null)
    {
        serviceCollection.AddScoped<INavigator>(provider => new Navigator(provider, logger.AsMaybe(), scheduler));
        serviceCollection.AddSingleton(factory);
        return serviceCollection;
    }
}