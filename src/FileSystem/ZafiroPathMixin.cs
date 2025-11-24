using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem;

public static class PathMixin
{
    public static string NameWithoutExtension(this Path path)
    {
        var last = path.RouteFragments.Last();
        var lastIndex = last.LastIndexOf('.');

        return lastIndex < 0 ? last : last[..lastIndex];
    }

    public static Maybe<string> Extension(this Path path)
    {
        var last = path.RouteFragments.Last();
        var lastIndex = last.LastIndexOf('.');

        return lastIndex < 0 ? Maybe<string>.None : last[(lastIndex+1)..];
    }
}