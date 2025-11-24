using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public interface ISectionSessionFactory
{
    Task<Result<SectionScope>> Create(ISection section);
}
