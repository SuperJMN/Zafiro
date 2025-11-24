using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Zafiro.FileSystem.Caching;

public class CachingZafiroFileSystem : IZafiroFileSystem
{
    private readonly HashSet<CopyOperationMetadata> hashSet;
    private readonly IZafiroFileSystem inner;

    public CachingZafiroFileSystem(IZafiroFileSystem inner)
    {
        this.inner = inner;
        Cache = new MemoryCache(new MemoryCacheOptions());
    }

    public Maybe<ILogger> Logger => inner.Logger;
    public MemoryCache Cache { get; }

    public Result<IZafiroFile> GetFile(Path path)
    {
        return inner.GetFile(path)
            .Map(file => (IZafiroFile) new CachingZafiroFile(file, this));
    }

    public Result<IZafiroDirectory> GetDirectory(Path path)
    {
        return inner.GetDirectory(path)
            .Map(dir => (IZafiroDirectory) new CachingZafiroDirectory(dir, this));
    }

    public void RemoveHash(Path getHash)
    {
        hashSet.RemoveWhere(d => d.Source.Equals(getHash));
    }
}