using System.Reactive.Concurrency;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zafiro.UI.Navigation.Sections;
using Zafiro.UI.Shell.Utils;

namespace Zafiro.UI.Navigation;

public static class NavigationServiceCollectionExtensions
{
    public static IServiceCollection AddSections(this IServiceCollection serviceCollection, Func<IServiceProvider, IEnumerable<ISection>> factory, ILogger? logger = null, IScheduler? scheduler = null)
    {
        serviceCollection.AddSingleton(factory);
        EnsureNavigatorRegistration(serviceCollection, logger, scheduler);
        return serviceCollection;
    }

    public static IServiceCollection AddSections(this IServiceCollection serviceCollection, Action<SectionsBuilder> configure, ILogger? logger = null, IScheduler? scheduler = null)
    {
        return serviceCollection.AddSections(provider =>
        {
            var builder = new SectionsBuilder();
            configure(builder);
            return builder.Build(provider, scheduler, logger.AsMaybe());
        }, logger, scheduler);
    }

    public static IServiceCollection AddSectionsFromAttributes(this IServiceCollection serviceCollection, Assembly assembly, ILogger? logger = null, IScheduler? scheduler = null)
    {
        return serviceCollection.AddSections(builder =>
        {
            foreach (var type in assembly.GetTypes().Where(Extensions.IsSection))
            {
                var sectionAttr = type.GetCustomAttribute<SectionAttribute>();
                if (sectionAttr is null)
                {
                    continue;
                }

                var baseName = type.Name.EndsWith("ViewModel", StringComparison.Ordinal)
                    ? type.Name[..^"ViewModel".Length]
                    : type.Name;

                var displayName = string.Concat(baseName.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
                var friendlyName = sectionAttr.FriendlyName ?? sectionAttr.Name ?? displayName;
                var contractType = sectionAttr.ContractType ?? type;
                var name = sectionAttr.Name ?? displayName;
                var iconSource = sectionAttr.Icon ?? "fa-window-maximize";

                var groupAttr = type.GetCustomAttribute<SectionGroupAttribute>();
                SectionGroup? group = null;
                if (groupAttr is not null)
                {
                    group = new SectionGroup(groupAttr.FriendlyName ?? groupAttr.Key);
                }

                var method = typeof(SectionsBuilder).GetMethod(nameof(SectionsBuilder.AddSection))!.MakeGenericMethod(contractType);
                method.Invoke(builder, new object?[] { name, friendlyName, new Icon { Source = iconSource }, group, sectionAttr.SortIndex });
            }
        }, logger, scheduler);
    }

    private static void EnsureNavigatorRegistration(IServiceCollection services, ILogger? logger, IScheduler? scheduler)
    {
        services.TryAddScoped<INavigator>(sp => new Navigator(sp, logger.AsMaybe(), scheduler ?? RxApp.MainThreadScheduler));
    }
}