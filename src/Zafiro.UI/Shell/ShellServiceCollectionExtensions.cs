using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zafiro.UI.Navigation;

namespace Zafiro.UI.Shell;

/// <summary>
/// Extension method to register the full Zafiro Shell infrastructure in one call.
/// </summary>
public static class ShellServiceCollectionExtensions
{
    /// <summary>
    /// Registers the complete Zafiro Shell infrastructure:
    /// <list type="bullet">
    ///     <item><see cref="IShell"/> and <see cref="IHierarchicalShell"/> → <see cref="Shell"/></item>
    ///     <item><see cref="INavigator"/> (scoped, one per section)</item>
    /// </list>
    /// After calling this method, the consumer only needs to call <c>services.AddAllSectionsFromAttributes()</c>
    /// from the generated code to register sections discovered via <c>[Section]</c> attributes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="logger">Optional Serilog logger for navigation diagnostics.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddZafiroShell(
        this IServiceCollection services,
        ILogger? logger = null)
    {
        services.AddSingleton<Shell>();
        services.AddSingleton<IShell>(provider => provider.GetRequiredService<Shell>());
        services.AddSingleton<IHierarchicalShell>(provider => provider.GetRequiredService<Shell>());
        services.TryAddScoped<INavigator>(sp =>
            new Navigator(sp, logger.AsMaybe(), RxSchedulers.MainThreadScheduler));

        return services;
    }
}
