using CSharpFunctionalExtensions;
using Serilog;
using Zafiro.Core;

namespace Zafiro.FileSystem.Smart;

public class SmartZafiroFileSystem : IZafiroFileSystem
{
    private readonly HashSet<CopyOperationMetadata> hashSet;
    private readonly IZafiroFileSystem inner;

    public SmartZafiroFileSystem(IZafiroFileSystem inner, Host host, HashSet<CopyOperationMetadata> hashSet)
    {
        this.inner = inner;
        Host = host;
        this.hashSet = hashSet;
    }

    public Host Host { get; }
    public Maybe<ILogger> Logger => inner.Logger;

    public Result<IZafiroFile> GetFile(Path path)
    {
        return inner.GetFile(path)
            .Map(file => (IZafiroFile) new SmartZafiroFile(file, this));
    }

    public Result<IZafiroDirectory> GetDirectory(Path path)
    {
        return inner.GetDirectory(path)
            .Map(dir => (IZafiroDirectory) new SmartZafiroDirectory(dir, this));
    }

    public void RemoveHash(Path getHash)
    {
        hashSet.RemoveWhere(d => d.Source.Equals(getHash));
    }

    public bool ContainsHash(IZafiroFile origin, IZafiroFile destination, Hash hash)
    {
        return hashSet.Contains(new CopyOperationMetadata(Host, origin.Path, destination.Path, hash));
    }

    public void AddHash(IZafiroFile source, IZafiroFile destination, Hash hash)
    {
        hashSet.Add(new CopyOperationMetadata(Host, source.Path, destination.Path, hash));
    }
}