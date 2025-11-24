using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public class ZafiroDirectory : IZafiroDirectory
{
    public ZafiroDirectory(Path path, IFileSystemRoot fileSystemRoot)
    {
        Path = path;
        FileSystem = fileSystemRoot;
        Changed = fileSystemRoot.Changed.Where(change => change.Path.Parent() == Path);
    }

    public Path Path { get; }
    public IFileSystemRoot FileSystem { get; }
    public Task<Result<DirectoryProperties>> Properties => FileSystem.GetDirectoryProperties(Path);

    public IObservable<FileSystemChange> Changed { get; }

    public Task<Result> Create()
    {
        return FileSystem.CreateDirectory(Path);
    }

    public Task<Result<IEnumerable<IZafiroFile>>> GetFiles()
    {
        return FileSystem.GetFiles(Path);
    }

    public Task<Result<IEnumerable<IZafiroDirectory>>> GetDirectories()
    {
        return FileSystem.GetDirectories(Path);
    }

    public Task<Result> Delete()
    {
        return FileSystem.DeleteDirectory(Path);
    }

    public Task<Result<bool>> Exists => FileSystem.ExistDirectory(Path);

    public override string ToString()
    {
        return $"{Path}";
    }
}