using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Zafiro.UI.Shell.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterAllSections(this IServiceCollection services, Assembly assembly)
    {
        var viewModelTypes = assembly.GetTypes().Where(Extensions.IsSection);

        foreach (var viewModelType in viewModelTypes)
        {
            services.AddTransient(viewModelType);
        }

        return services;
    }
}
