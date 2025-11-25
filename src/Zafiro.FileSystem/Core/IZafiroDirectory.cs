using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Core;

public interface IZafiroDirectory
{
    Path Path { get; }
    Task<Result<bool>> Exists { get; }
    IFileSystemRoot FileSystem { get; }
    Task<Result<DirectoryProperties>> Properties { get; }
    IObservable<FileSystemChange> Changed { get; }
    Task<Result> Create();
    Task<Result<IEnumerable<IZafiroFile>>> GetFiles();
    Task<Result<IEnumerable<IZafiroDirectory>>> GetDirectories();
    Task<Result> Delete();
}