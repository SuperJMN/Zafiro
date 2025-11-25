using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Core;

public interface IZafiroFileSystem
{
    Task<Result> CreateFile(Path path);
    IObservable<byte> GetFileContents(Path path);
    Task<Result> SetFileContents(Path path, IObservable<byte> bytes, CancellationToken cancellationToken);
    Task<Result> CreateDirectory(Path path);
    Task<Result<FileProperties>> GetFileProperties(Path path);
    Task<Result<IDictionary<HashMethod, byte[]>>> GetHashes(Path path);
    Task<Result<DirectoryProperties>> GetDirectoryProperties(Path path);
    Task<Result<IEnumerable<Path>>> GetFilePaths(Path path, CancellationToken ct = default);
    Task<Result<IEnumerable<Path>>> GetDirectoryPaths(Path path, CancellationToken ct = default);
    Task<Result<bool>> ExistDirectory(Path path);
    Task<Result<bool>> ExistFile(Path path);
    Task<Result> DeleteFile(Path path);
    Task<Result> DeleteDirectory(Path path);
    Task<Result<Stream>> GetFileData(Path path);
    Task<Result> SetFileData(Path path, Stream stream, CancellationToken ct = default);
}