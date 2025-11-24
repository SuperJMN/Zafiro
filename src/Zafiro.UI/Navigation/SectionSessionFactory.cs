using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public sealed class SectionSessionFactory(IServiceProvider provider) : ISectionSessionFactory
{
    public async Task<Result<SectionScope>> Create(ISection section)
    {
        var session = new SectionScope(provider);

        if (section is IInitializableSection initializable)
        {
            await Task.Yield();

            var result = await initializable.Initialize(session.Navigator);
            if (result.IsFailure)
            {
                session.Dispose();
                return Result.Failure<SectionScope>(result.Error);
            }
        }

        return Result.Success(session);
    }
}
