using System.Reflection;
using Zafiro.UI.Navigation;
using Zafiro.UI.Shell.Utils;

namespace Zafiro.UI.Shell;

public class ShellProperties(object header, Func<object, IObservable<object?>>? getContentHeader = null)
{
    public object Header { get; } = header;
    public Func<object, IObservable<object?>> GetHeader { get; } = getContentHeader ?? DefaultGetContentHeader;

    private static IObservable<object?> DefaultGetContentHeader(object content)
    {
        if (content is INavigator navigator)
        {
            return navigator.Content.Select(o => o?.GetType().GetCustomAttribute<SectionAttribute>()?.Name);
        }

        return Observable.Return<object?>(null);
    }
}