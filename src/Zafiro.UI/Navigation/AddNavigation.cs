using System.Reactive.Concurrency;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.UI.Navigation.Sections;
using Zafiro.UI.Shell.Utils;

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

    public static IServiceCollection RegisterSections(this IServiceCollection serviceCollection, Action<SectionsBuilder> configure, ILogger? logger = null, IScheduler? scheduler = null)
    {
        return serviceCollection.RegisterNavigationRoots(provider =>
        {
            var builder = new SectionsBuilder();
            configure(builder);
            return builder.Build(provider);
        }, logger, scheduler);
    }

    public static IServiceCollection RegisterSectionsFromAttributes(this IServiceCollection serviceCollection, Assembly assembly, ILogger? logger = null, IScheduler? scheduler = null)
    {
        return serviceCollection.RegisterNavigationRoots(provider =>
        {
            var roots = new List<INavigationRoot>();

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
                var friendlyName = sectionAttr.Name ?? displayName;
                var contractType = sectionAttr.ContractType ?? type;
                var iconSource = sectionAttr.Icon ?? "fa-window-maximize";

                var groupAttr = type.GetCustomAttribute<SectionGroupAttribute>();
                SectionGroup? group = null;
                if (groupAttr is not null)
                {
                    group = new SectionGroup(groupAttr.FriendlyName ?? groupAttr.Key);
                }

                var rootType = typeof(NavigationRoot<>).MakeGenericType(contractType);
                var root = (INavigationRoot)Activator.CreateInstance(
                    rootType,
                    displayName,
                    provider,
                    new Icon { Source = iconSource },
                    group,
                    friendlyName)!;

                root.SortOrder = sectionAttr.SortIndex;
                roots.Add(root);
            }

            return roots;
        }, logger, scheduler);
    }
}