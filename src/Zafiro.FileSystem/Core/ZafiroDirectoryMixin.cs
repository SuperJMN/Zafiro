using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public static class ZafiroDirectoryMixin
{
    public static async Task<Result<Maybe<IZafiroDirectory>>> DescendantDirectory(this IZafiroDirectory directory, Path subDirectoryPath)
    {
        // Base
        if (!subDirectoryPath.RouteFragments.Any())
        {
            return Result.Success(Maybe.From(directory));
        }

        // Recursive
        var dirsResult = await directory.GetDirectories().ConfigureAwait(false);
        var tryFindResult = dirsResult.Map(r => r.TryFirst(x => DoMatch(x, subDirectoryPath)).ToResult($"Directory not found: {subDirectoryPath}"));
        var getDescendantResult = await tryFindResult.Bind(maybe => maybe.Bind(subDir => subDir.DescendantDirectory(NextPath(subDirectoryPath)))).ConfigureAwait(false);
        return getDescendantResult;
    }

    private static Path NextPath(Path subDirectoryPath)
    {
        return new Path(subDirectoryPath.RouteFragments.Skip(1));
    }

    private static bool DoMatch(IZafiroDirectory zafiroDirectory, Path subDirectoryPath)
    {
        return zafiroDirectory.Path.Name() == subDirectoryPath.RouteFragments.First();
    }

    public static IZafiroFile GetSingleFile(this IZafiroDirectory zafiroDirectory, string filename)
    {
        var zafiroPath = zafiroDirectory.Path.Combine(filename);
        return new ZafiroFile(zafiroPath, zafiroDirectory.FileSystem);
    }

    public static Task<Result<Maybe<IZafiroFile>>> GetFile(this IZafiroDirectory zafiroDirectory, string filename)
    {
        var maybeGetFile = zafiroDirectory
            .GetFiles()
            .Map(r => r.TryFirst(file => string.Equals(file.Path.Name(), filename, StringComparison.OrdinalIgnoreCase)));

        return maybeGetFile;
    }

    public static Task<Result<IZafiroFile>> GetFromPath(this IZafiroDirectory origin, Path path)
    {
        return Task.FromResult(Result.Success(origin.FileSystem.GetFile(origin.Path.Combine(path))));
    }
}