using System.Reactive.Linq;
using System.Reactive.Subjects;
using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Mutable;

public abstract class DirectoryBase : IMutableDirectory
{
    private readonly Subject<IMutableDirectory> directoryCreated = new();
    private readonly Subject<string> directoryDeleted = new();
    private readonly Subject<IMutableFile> fileCreated = new();
    private readonly Subject<string> fileDeleted = new();

    public IObservable<IMutableFile> FileCreated => fileCreated.AsObservable();
    public IObservable<IMutableDirectory> DirectoryCreated => directoryCreated.AsObservable();
    public IObservable<string> FileDeleted => fileDeleted.AsObservable();
    public IObservable<string> DirectoryDeleted => directoryDeleted.AsObservable();

    public abstract string Name { get; }

    public Task<Result<IMutableDirectory>> CreateSubdirectory(string name)
    {
        return CreateSubdirectoryCore(name)
            .Tap(d => directoryCreated.OnNext(d));
    }

    public Task<Result> DeleteFile(string name)
    {
        return DeleteFileCore(name)
            .Tap(() => fileDeleted.OnNext(name));
    }

    public Task<Result> DeleteSubdirectory(string name)
    {
        return DeleteSubdirectoryCore(name)
            .Tap(() => directoryDeleted.OnNext(name));
    }

    public Task<Result<IMutableFile>> CreateFile(string entryName)
    {
        return CreateFileCore(entryName)
            .Tap(f => fileCreated.OnNext(f));
    }

    public abstract bool IsHidden { get; }

    public abstract Task<Result<IEnumerable<IMutableNode>>> GetChildren(CancellationToken cancellationToken = default);
    public abstract Task<Result<bool>> HasFile(string name);
    public abstract Task<Result<bool>> HasSubdirectory(string name);
    protected abstract Task<Result<IMutableDirectory>> CreateSubdirectoryCore(string name);
    protected abstract Task<Result> DeleteFileCore(string name);
    protected abstract Task<Result> DeleteSubdirectoryCore(string name);
    protected abstract Task<Result<IMutableFile>> CreateFileCore(string entryName);
}