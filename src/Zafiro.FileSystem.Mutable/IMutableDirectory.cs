using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Mutable;

public interface IMutableDirectory : IMutableNode
{
    IObservable<IMutableFile> FileCreated { get; }
    IObservable<IMutableDirectory> DirectoryCreated { get; }
    IObservable<string> FileDeleted { get; }
    IObservable<string> DirectoryDeleted { get; }
    Task<Result> DeleteFile(string name);
    Task<Result> DeleteSubdirectory(string name);
    Task<Result<IMutableFile>> CreateFile(string entryName);
    Task<Result<IMutableDirectory>> CreateSubdirectory(string name);
    Task<Result<IEnumerable<IMutableNode>>> GetChildren(CancellationToken cancellationToken = default);
    Task<Result<bool>> HasFile(string name);
    Task<Result<bool>> HasSubdirectory(string name);
}