using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Smart;

public class SmartZafiroDirectory : IZafiroDirectory
{
    private readonly IZafiroDirectory inner;
    private readonly SmartZafiroFileSystem filesystem;

    public SmartZafiroDirectory(IZafiroDirectory inner, SmartZafiroFileSystem filesystem)
    {
        this.inner = inner;
        this.filesystem = filesystem;
    }

    public IEnumerable<IZafiroFile> Files => inner.Files.Select(f => new SmartZafiroFile(f, filesystem));

    public IEnumerable<IZafiroDirectory> Directories => inner.Directories.Select(d => new SmartZafiroDirectory(d, filesystem));

    public Path Path => inner.Path;

    public IZafiroFileSystem FileSystem => filesystem;

    public Result<IZafiroFile> GetFile(string name)
    {
        return inner.GetFile(name).Map(r => (IZafiroFile)new SmartZafiroFile(r, filesystem));
    }
}