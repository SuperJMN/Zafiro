using MoreLinq;

namespace Zafiro.FileSystem;

public static class ZafiroFileSystemMixin
{
    public static IEnumerable<IZafiroFile> GetAllFiles(this IZafiroDirectory origin)
    {
        return MoreEnumerable.TraverseBreadthFirst(origin, dir => dir.Directories)
            .SelectMany(r => r.Files);
    }

    public static Path MakeRelativeTo(this Path path, Path relativeTo)
    {
        var relativePathChunks =
            relativeTo.RouteFragments
                .ZipLongest(path.RouteFragments, (x, y) => (x, y))
                .SkipWhile(x => x.x == x.y)
                .Select(x => { return x.x is null ? new[] {x.y} : new[] {"..", x.y}; })
                .Transpose()
                .SelectMany(x => x)
                .Where(x => x is not default(string));

        return new Path(relativePathChunks);
    }


    public static Path Combine(this Path self, Path path)
    {
        return new Path(self.RouteFragments.Concat(path.RouteFragments));
    }

    public static async Task<byte[]> ReadAllBytes(this IZafiroFile file)
    {
        using (var memoryStream = new MemoryStream())
        using (var sourceStream = await file.OpenRead())
        {
            await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            return memoryStream.ToArray();
        }
    }
}