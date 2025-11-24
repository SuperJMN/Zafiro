using System.Reactive.Linq;
using System.Reactive.Subjects;
using CSharpFunctionalExtensions;
using MoreLinq;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public class ObservableFileSystem : IObservableFileSystem
{
    private readonly Subject<FileSystemChange> changed = new();
    private readonly IZafiroFileSystem fs;

    public ObservableFileSystem(IZafiroFileSystem fs)
    {
        this.fs = fs;
    }

    public IObservable<FileSystemChange> Changed => changed.AsObservable();

    public Task<Result<bool>> ExistFile(Path path)
    {
        return fs.ExistFile(path);
    }

    public Task<Result> DeleteFile(Path path)
    {
        return fs.DeleteFile(path).Tap(() => changed.OnNext(new FileSystemChange(path, Change.FileDeleted)));
    }

    public Task<Result> DeleteDirectory(Path path)
    {
        return fs.DeleteDirectory(path).Tap(() => changed.OnNext(new FileSystemChange(path, Change.DirectoryDeleted)));
    }

    public Task<Result<Stream>> GetFileData(Path path)
    {
        return fs.GetFileData(path);
    }

    public Task<Result> SetFileData(Path path, Stream stream, CancellationToken cancellationToken)
    {
        return fs
            .SetFileData(path, stream, cancellationToken)
            .Tap(() => NotifyFileCreate(path));
    }

    public Task<Result> CreateFile(Path path)
    {
        return fs
            .CreateFile(path)
            .Tap(() => NotifyFileCreate(path));
    }

    public IObservable<byte> GetFileContents(Path path)
    {
        return fs.GetFileContents(path);
    }

    public async Task<Result> SetFileContents(Path path, IObservable<byte> bytes, CancellationToken cancellationToken)
    {
        var changes = await Notifications.BeforeFileCreate(fs, path).ConfigureAwait(false);

        return await fs
            .SetFileContents(path, bytes, cancellationToken)
            .Tap(() => changes.ForEach(r => changed.OnNext(r)))
            .ConfigureAwait(false);
    }

    public Task<Result> CreateDirectory(Path path)
    {
        return fs.CreateDirectory(path).Tap(() => changed.OnNext(new FileSystemChange(path, Change.DirectoryCreated)));
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

    public Task<Result<bool>> ExistDirectory(Path path)
    {
        return fs.ExistDirectory(path);
    }

    public Task<Result<bool>> ExistsDirectory(Path path)
    {
        return fs.ExistDirectory(path);
    }

    private void NotifyFileCreate(Path path)
    {
        path
            .Parents()
            .Select(zafiroPath => new FileSystemChange(zafiroPath, Change.DirectoryCreated))
            .Append(new FileSystemChange(path, Change.FileCreated))
            .ForEach(changed.OnNext);
    }
}