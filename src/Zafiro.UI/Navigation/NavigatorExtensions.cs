namespace Zafiro.UI.Navigation;

/// <summary>
/// Extension methods for INavigator
/// </summary>
public static class NavigatorExtensions
{
    /// <summary>
    /// Navigate to a specific type
    /// </summary>
    /// <typeparam name="T">Type to navigate to</typeparam>
    /// <param name="navigator">The navigator</param>
    /// <returns>Operation result</returns>
    public static Task<Result<Unit>> Go<T>(this INavigator navigator) where T : notnull
    {
        return navigator.Go(typeof(T));
    }
}