using CSharpFunctionalExtensions;
using MoreLinq;

namespace Zafiro.DivineBytes;

public static class PathMixin
{
    public static Path MakeRelativeTo(this Path path, Path relativeTo)
    {
        var relativePathChunks =
            relativeTo.RouteFragments
                .ZipLongest(path.RouteFragments, (x, y) => (x, y))
                .SkipWhile(x => x.x == x.y)
                .Select(x => x.x is null ? new[] { x.y } : new[] { "..", x.y })
                .Transpose()
                .SelectMany(x => x)
                .OfType<string>();

        return new Path(relativePathChunks);
    }

    public static Path Combine(this Path self, Path path)
    {
        return new Path(self.RouteFragments.Concat(path.RouteFragments));
    }

    public static Maybe<Path> Parent(this Path path)
    {
        if (path == Path.Empty)
        {
            return Maybe<Path>.None;
        }

        return new Path(path.RouteFragments.SkipLast(1));
    }

    public static string Name(this Path path)
    {
        return path.RouteFragments.LastOrDefault() ?? "";
    }

    public static string NameWithoutExtension(this Path path)
    {
        var last = path.RouteFragments.Last();
        var lastIndex = last.LastIndexOf('.');

        return lastIndex < 0 ? last : last[..lastIndex];
    }

    public static Maybe<string> Extension(this Path path)
    {
        var name = path.Name();
        var dotPos = name.LastIndexOf('.');
        return dotPos < 0 ? Maybe<string>.None : name[(dotPos + 1)..];
    }

    public static IEnumerable<Path> Parents(this Path path)
    {
        if (path == Path.Empty)
        {
            return Enumerable.Empty<Path>();
        }

        return path.Parent().Match(parent => parent.Parents().Append(parent), Enumerable.Empty<Path>);
    }

    public static IEnumerable<Path> ParentsAndSelf(this Path path)
    {
        return path.Parents().Append(path);
    }
}
