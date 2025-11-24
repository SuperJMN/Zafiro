using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;
using Zafiro.Core.IO;
using Zafiro.Core.Mixins;

namespace Zafiro.FileSystem.Caching;

public class CachingZafiroFile : IZafiroFile
{
    private readonly CachingZafiroFileSystem fileSystem;
    private readonly IZafiroFile inner;

    public CachingZafiroFile(IZafiroFile inner, CachingZafiroFileSystem fileSystem)
    {
        this.inner = inner;
        this.fileSystem = fileSystem;
    }

    public Path Path => inner.Path;
    public string Name => inner.Name;

    public Task<Result> CopyTo(IZafiroFile destination)
    {
        return inner.CopyTo(destination);
    }

    public Task<Stream> OpenWrite()
    {
        return inner.OpenWrite();
    }

    public Result Delete()
    {
        fileSystem.Cache.Remove(Path.Path);
        return Result.Success();
    }

    public async Task<Stream> OpenRead()
    {
        var contents = await fileSystem.Cache.GetOrCreate(Path.Path, async _ =>
        {
            var openRead = await inner.OpenRead();
            var contents = await openRead.ReadBytes();
            return contents;
        });

        return new MemoryStream(contents);
    }

    public IZafiroFileSystem FileSystem => fileSystem;

    public override string? ToString()
    {
        return inner.ToString();
    }
}