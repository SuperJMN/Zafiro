using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Core;

public interface IZafiroFile
{
    IObservable<byte> Contents { get; }
    Task<Result<bool>> Exists { get; }
    Path Path { get; }
    Task<Result<FileProperties>> Properties { get; }
    Task<Result<IDictionary<HashMethod, byte[]>>> Hashes { get; }
    IFileSystemRoot FileSystem { get; }
    Task<Result> Delete();
    Task<Result> SetContents(IObservable<byte> contents, CancellationToken cancellationToken = default);
    Task<Result<Stream>> GetData();
    Task<Result> SetData(Stream stream, CancellationToken cancellationToken = default);
}