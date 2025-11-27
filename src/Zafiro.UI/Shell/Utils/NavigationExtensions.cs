using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.UI.Navigation;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell.Utils;

public static class NavigationExtensions
{
    public static IServiceCollection AddAllSections(this IServiceCollection services, Assembly assembly)
    {
        services.RegisterNavigationRoots(provider =>
        {
            var sections = from sectionType in assembly.GetTypes().Where(Extensions.IsSection)
                let sectionAttribute = sectionType.GetCustomAttribute<SectionAttribute>()
                select new { sectionType = sectionType, sectionAttribute };

            var roots = new List<INavigationRoot>();

            foreach (var viewModelType in sections.OrderBy(arg => arg.sectionAttribute.SortIndex))
            {
                var type = viewModelType.sectionType;
                var icon = viewModelType.sectionAttribute.Icon;

                string sectionName = type.Name.Replace("ViewModel", "");
                string formattedName = string.Concat(sectionName.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');

                var friendlyName = viewModelType.sectionAttribute.FriendlyName ?? viewModelType.sectionAttribute.Name ?? formattedName;
                var name = viewModelType.sectionAttribute.Name ?? formattedName;
                var navigationRootType = typeof(NavigationRoot<>).MakeGenericType(type);
                var navigationRoot = (INavigationRoot)Activator.CreateInstance(
                    navigationRootType,
                    name,
                    provider,
                    new Icon { Source = icon ?? "fa-window-maximize" },
                    null,
                    friendlyName)!;
                navigationRoot.SortOrder = viewModelType.sectionAttribute.SortIndex;
                roots.Add(navigationRoot);
            }

            return roots;
        }, scheduler: RxApp.MainThreadScheduler);

        return services;
    }
}
