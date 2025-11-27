using System.Reactive.Threading.Tasks;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Navigation;

public sealed class SectionSessionFactory(IServiceProvider provider) : ISectionSessionFactory
{
    public async Task<Result<SectionScope>> Create(ISection section)
    {
        if (section is INavigationRoot navigationRoot)
        {
            return await CreateFromNavigationRoot(navigationRoot);
        }

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

    private static async Task<Result<SectionScope>> CreateFromNavigationRoot(INavigationRoot navigationRoot)
    {
        try
        {
            await navigationRoot.Content.Take(1).ToTask();
            return Result.Success(new SectionScope(navigationRoot.Navigator, navigationRoot as IDisposable));
        }
        catch (Exception ex)
        {
            (navigationRoot as IDisposable)?.Dispose();
            return Result.Failure<SectionScope>(ex.Message);
        }
    }
}