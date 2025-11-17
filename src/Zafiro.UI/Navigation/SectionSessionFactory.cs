using CSharpFunctionalExtensions;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public sealed class SectionSessionFactory : ISectionSessionFactory
{
    private readonly IServiceProvider provider;

    public SectionSessionFactory(IServiceProvider provider)
    {
        this.provider = provider;
    }

    public async Task<Result<SectionScope>> Create(ISection section)
    {
        var session = new SectionScope(provider, section.RootType);

        // Defer the initial navigation to avoid constructor-time cycles
        await Task.Yield();

        var result = await session.Navigator.Go(section.RootType);
        if (result.IsFailure)
        {
            session.Dispose();
            return Result.Failure<SectionScope>(result.Error);
        }

        return Result.Success(session);
    }
}