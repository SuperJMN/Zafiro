using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Core;

public class FileSystemRoot : IFileSystemRoot
{
    private readonly IObservableFileSystem fs;

    public FileSystemRoot(IObservableFileSystem fs)
    {
        this.fs = fs;
    }

    public IZafiroFile GetFile(Path path)
    {
        return new ZafiroFile(path, this);
    }

    public IZafiroDirectory GetDirectory(Path path)
    {
        return new ZafiroDirectory(path, this);
    }

    public Task<Result<IEnumerable<IZafiroFile>>> GetFiles(Path path, CancellationToken ct = default)
    {
        return fs.GetFilePaths(path, ct).Map(paths => paths.Select(zafiroPath => (IZafiroFile)new ZafiroFile(zafiroPath, this)));
    }

    public Task<Result<IEnumerable<IZafiroDirectory>>> GetDirectories(Path path, CancellationToken ct = default)
    {
        return fs.GetDirectoryPaths(path, ct).Map(paths => paths.Select(zafiroPath => (IZafiroDirectory)new ZafiroDirectory(zafiroPath, this)));
    }

    public Task<Result<Stream>> GetFileData(Path path)
    {
        return fs.GetFileData(path);
    }

    public Task<Result> SetFileData(Path path, Stream stream, CancellationToken ct = default)
    {
        return fs.SetFileData(path, stream, ct);
    }

    public Task<Result<bool>> ExistFile(Path path)
    {
        return fs.ExistFile(path);
    }

    public Task<Result> DeleteFile(Path path)
    {
        return fs.DeleteFile(path);
    }

    public Task<Result> DeleteDirectory(Path path)
    {
        return fs.DeleteDirectory(path);
    }

    public Task<Result<bool>> ExistDirectory(Path path)
    {
        return fs.ExistDirectory(path);
    }

    public Task<Result> CreateFile(Path path)
    {
        return fs.CreateFile(path);
    }

    public IObservable<byte> GetFileContents(Path path)
    {
        return fs.GetFileContents(path);
    }

    public Task<Result> SetFileContents(Path path, IObservable<byte> bytes, CancellationToken cancellationToken)
    {
        return fs.SetFileContents(path, bytes, cancellationToken);
    }

    public Task<Result> CreateDirectory(Path path)
    {
        return fs.CreateDirectory(path);
    }

    public Task<Result<FileProperties>> GetFileProperties(Path path)
    {
        return fs.GetFileProperties(path);
    }

    public Task<Result<IDictionary<HashMethod, byte[]>>> GetHashes(Path path)
    {
        return fs.GetHashes(path);
    }

    public Task<Result<DirectoryProperties>> GetDirectoryProperties(Path path)
    {
        return fs.GetDirectoryProperties(path);
    }

    public Task<Result<IEnumerable<Path>>> GetFilePaths(Path path, CancellationToken ct = default)
    {
        return fs.GetFilePaths(path, ct);
    }

    public Task<Result<IEnumerable<Path>>> GetDirectoryPaths(Path path, CancellationToken ct = default)
    {
        return fs.GetDirectoryPaths(path, ct);
    }

    public IObservable<FileSystemChange> Changed => fs.Changed;
}