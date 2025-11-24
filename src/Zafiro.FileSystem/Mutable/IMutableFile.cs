using System.Reactive.Concurrency;
using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Mutable;

public interface IMutableFile : IMutableNode
{
    Task<Result> SetContents(IByteSource data, IScheduler? scheduler = null, CancellationToken cancellationToken = default);
    Task<Result<IByteSource>> GetContents();
}