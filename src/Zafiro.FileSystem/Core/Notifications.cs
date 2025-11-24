using CSharpFunctionalExtensions;
using Zafiro.CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public class Notifications
{
    public static Task<IEnumerable<FileSystemChange>> BeforeFileCreate(IZafiroFileSystem fs, Path path)
    {
        var fileExist = fs.ExistFile(path).Not().Map(b => b ? new[] { new FileSystemChange(path, Change.FileCreated) } : Enumerable.Empty<FileSystemChange>());

        var parents = path
            .Parents()
            .Select(dir => fs.ExistDirectory(dir).Map(b => (File: dir, Exist: b)))
            .Combine(t => t.Where(tuple => !tuple.Exist).Select(r => new FileSystemChange(r.File, Change.DirectoryCreated)));

        return fileExist.CombineAndMap(parents, (a, b) => a.Concat(b)).GetValueOrDefault(Enumerable.Empty<FileSystemChange>);
    }
}