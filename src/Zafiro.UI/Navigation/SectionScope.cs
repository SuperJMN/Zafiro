using Microsoft.Extensions.DependencyInjection;

namespace Zafiro.UI.Navigation;

public sealed class SectionScope : ISectionScope
{
    private readonly IServiceScope scope;

    public SectionScope(IServiceProvider provider)
    {
        scope = provider.CreateScope();
        Navigator = scope.ServiceProvider.GetRequiredService<INavigator>();
    }

    public void Dispose()
    {
        scope.Dispose();
    }

    public INavigator Navigator { get; }
}
