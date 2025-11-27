using Microsoft.Extensions.DependencyInjection;

namespace Zafiro.UI.Navigation;

public sealed class SectionScope : ISectionScope
{
    private readonly IDisposable? disposable;

    public SectionScope(IServiceProvider provider)
    {
        var createdScope = provider.CreateScope();
        Navigator = createdScope.ServiceProvider.GetRequiredService<INavigator>();
        disposable = createdScope;
    }

    public SectionScope(INavigator navigator, IDisposable? disposable = null)
    {
        Navigator = navigator;
        this.disposable = disposable;
    }

    public void Dispose()
    {
        disposable?.Dispose();
    }

    public INavigator Navigator { get; }
}