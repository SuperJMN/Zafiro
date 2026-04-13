using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

internal sealed class SectionFactory(Func<IServiceProvider, IEnumerable<ISection>> factory)
{
    public IEnumerable<ISection> Create(IServiceProvider provider) => factory(provider);
}
