using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Core;

public class ZafiroFile : IZafiroFile
{
    public ZafiroFile(Path path, IFileSystemRoot fileSystemRoot)
    {
        Path = path;
        FileSystem = fileSystemRoot;
    }

    public IFileSystemRoot FileSystem { get; }
    public IObservable<byte> Contents => FileSystem.GetFileContents(Path);
    public Task<Result<bool>> Exists => FileSystem.ExistFile(Path);
    public Path Path { get; }
    public Task<Result<IDictionary<HashMethod, byte[]>>> Hashes => FileSystem.GetHashes(Path);

    public Task<Result> Delete()
    {
        return FileSystem.DeleteFile(Path);
    }

    public Task<Result> SetContents(IObservable<byte> contents, CancellationToken cancellationToken = default)
    {
        return FileSystem.SetFileContents(Path, contents, cancellationToken);
    }

    public Task<Result<Stream>> GetData()
    {
        return FileSystem.GetFileData(Path);
    }

    public Task<Result> SetData(Stream stream, CancellationToken cancellationToken = default)
    {
        return FileSystem.SetFileData(Path, stream, cancellationToken);
    }

    public Task<Result<FileProperties>> Properties => FileSystem.GetFileProperties(Path);

    public override string ToString()
    {
        return Path;
    }
}