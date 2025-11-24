using System.IO.Abstractions;
using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem;

public class ZafiroDirectory : IZafiroDirectory
{
    private readonly IDirectoryInfo directoryInfo;
    private readonly ZafiroFileSystem zafiroFileSystem;

    public ZafiroDirectory(IDirectoryInfo directoryInfo, ZafiroFileSystem zafiroFileSystem)
    {
        this.directoryInfo = directoryInfo;
        this.zafiroFileSystem = zafiroFileSystem;
    }

    public IEnumerable<IZafiroFile> Files => directoryInfo.EnumerateFiles().Select(info => new ZafiroFile(info, zafiroFileSystem));

    public IEnumerable<IZafiroDirectory> Directories =>
        directoryInfo.EnumerateDirectories().Select(x => new ZafiroDirectory(x, zafiroFileSystem));

    public Path Path => new(directoryInfo.FullName.Split(directoryInfo.FileSystem.Path.DirectorySeparatorChar));
    public IZafiroFileSystem FileSystem => zafiroFileSystem;
    public Result<IZafiroFile> GetFile(string name)
    {
        return FileSystem.GetFile(this.Path.Combine(name));
    }
}