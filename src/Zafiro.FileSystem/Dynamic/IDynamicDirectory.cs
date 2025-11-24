using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;
using DynamicData;
using Zafiro.FileSystem.Core;
using Zafiro.FileSystem.Readonly;

namespace Zafiro.FileSystem.Dynamic;

public interface IDynamicDirectory : INamed
{
    IObservable<IChangeSet<IDynamicDirectory, string>> Directories { get; }
    IObservable<IChangeSet<IFile, string>> Files { get; }
    Task<Result> DeleteFile(string name);
    Task<Result> AddOrUpdateFile(params IFile[] files);
}