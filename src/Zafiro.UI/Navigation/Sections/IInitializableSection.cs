using Zafiro.UI.Navigation;

namespace Zafiro.UI.Navigation.Sections;

internal interface IInitializableSection
{
    Task<Result<Unit>> Initialize(INavigator navigator);
}
