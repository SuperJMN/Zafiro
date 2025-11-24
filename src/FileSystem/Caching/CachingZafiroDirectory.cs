using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Caching;

public class CachingZafiroDirectory : IZafiroDirectory
{
    private readonly IZafiroDirectory inner;
    private readonly CachingZafiroFileSystem filesystem;

    public CachingZafiroDirectory(IZafiroDirectory inner, CachingZafiroFileSystem filesystem)
    {
        this.inner = inner;
        this.filesystem = filesystem;
    }

    public IEnumerable<IZafiroFile> Files => inner.Files.Select(f => new CachingZafiroFile(f, filesystem));

    public IEnumerable<IZafiroDirectory> Directories => inner.Directories.Select(d => new CachingZafiroDirectory(d, filesystem));

    public Path Path => inner.Path;

    public IZafiroFileSystem FileSystem => filesystem;

    public Result<IZafiroFile> GetFile(string name)
    {
        return inner.GetFile(name).Map(r => (IZafiroFile)new CachingZafiroFile(r, filesystem));
    }
}