using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Core;

public interface IFileSystemRoot : IObservableFileSystem
{
    IZafiroFile GetFile(Path path);
    IZafiroDirectory GetDirectory(Path path);
    Task<Result<IEnumerable<IZafiroFile>>> GetFiles(Path path, CancellationToken ct = default);
    Task<Result<IEnumerable<IZafiroDirectory>>> GetDirectories(Path path, CancellationToken ct = default);
}