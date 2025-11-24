using System.Reactive;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using Renci.SshNet;
using Zafiro.CSharpFunctionalExtensions;
using Zafiro.Reactive;

namespace Zafiro.FileSystem.Sftp;

public class SftpFileSystem : IZafiroFileSystem
{
    private readonly SftpClient sftpClient;

    public SftpFileSystem(SftpClient sftpClient)
    {
        this.sftpClient = sftpClient;
    }

    public async Task<Result> CreateFile(Path path) => Result.Try(() => sftpClient.Create(FromZafiroToFileSystem(path)));

    private static string FromZafiroToFileSystem(Path path)
    {
        return "/" + path.Path;
    }

    public static Path FromFileSystemToPath(string path)
    {
        return path.StartsWith("/") ? path[1..] : path;
    }

    public IObservable<byte> GetFileContents(Path path)
    {
        return Observable.Using(() => sftpClient.OpenRead(FromZafiroToFileSystem(path)), stream => stream.ToObservable());
    }

    public Task<Result> SetFileContents(Path path, IObservable<byte> bytes, CancellationToken cancellationToken)
    {
        return EnsureDirectoryExists(path.Parent())
            .Bind(async () => await Observable.Using(() => bytes.ToStream(), stream => Observable.FromAsync(ct => Result.Try(() => sftpClient.UploadFileAsync(FromFileSystemToPath(path), stream)))));
    }

    public async Task<Result> CreateDirectory(Path path) => Result.Try(() => sftpClient.Create(FromZafiroToFileSystem(path)));

    public async Task<Result<FileProperties>> GetFileProperties(Path path)
    {
        return Result
            .Try(() => sftpClient.GetAttributes(FromZafiroToFileSystem(path)))
            .Map(f => new FileProperties(path.Name().StartsWith("."), DateTimeOffset.MinValue, f.Size));
    }

    public async Task<Result<IDictionary<HashMethod, byte[]>>> GetHashes(Path path)
    {
        return new Dictionary<HashMethod, byte[]>();
    }

    public async Task<Result<DirectoryProperties>> GetDirectoryProperties(Path path)
    {
        return Result
            .Try(() => sftpClient.GetAttributes(FromZafiroToFileSystem(path)))
            .Map(_ => new DirectoryProperties(path.Name().StartsWith("."), DateTimeOffset.MinValue));
    }

    public Task<Result<IEnumerable<Path>>> GetFilePaths(Path path, CancellationToken ct = default)
    {
        return Result.Try(() => sftpClient.ListDirectoryAsync(FromZafiroToFileSystem(path)))
            .Map(files => files
                .Where(file => !file.IsDirectory)
                .Select(file => FromFileSystemToPath(file.FullName)));
    }

    public Task<Result<IEnumerable<Path>>> GetDirectoryPaths(Path path, CancellationToken ct = default)
    {
        return Result.Try(() => sftpClient.ListDirectoryAsync(FromZafiroToFileSystem(path)))
            .Map(files => files
                .Where(file => file.IsDirectory)
                .Select(file => FromFileSystemToPath(file.FullName)));
    }

    public async Task<Result<bool>> ExistDirectory(Path path) => Result.Try(() => sftpClient.Exists(FromFileSystemToPath(path)));

    public async Task<Result<bool>> ExistFile(Path path) => Result.Try(() => sftpClient.Exists(FromFileSystemToPath(path)));

    public async  Task<Result> DeleteFile(Path path) => Result.Try(() => sftpClient.DeleteFile(FromFileSystemToPath(path)));

    public async Task<Result> DeleteDirectory(Path path) => Result.Try(() => sftpClient.DeleteDirectory(FromFileSystemToPath(path)));
    public Task<Result<Stream>> GetFileData(Path path) => Task.FromResult(Result.Try(() => (Stream)sftpClient.OpenRead(FromZafiroToFileSystem(path))));

    public Task<Result> SetFileData(Path path, Stream stream, CancellationToken ct = default)
    {
        return EnsureDirectoryExists(path.Parent())
            .Map(() => sftpClient.UploadFileAsync(FromFileSystemToPath(path), stream));
    }

    private Result EnsureDirectoryExists(Path path)
    {
        return Result.Try(() =>
        {
            if (!sftpClient.Exists(path))
            {
                EnsureDirectoryExists(path.Parent());
                sftpClient.CreateDirectory(path);
            }
        });
    }
}