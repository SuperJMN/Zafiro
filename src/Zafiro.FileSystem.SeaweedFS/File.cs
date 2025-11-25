using System.Reactive.Concurrency;
using Zafiro.CSharpFunctionalExtensions;
using Zafiro.DivineBytes;
using Zafiro.Reactive;

namespace Zafiro.FileSystem.SeaweedFS;

public class File(Path path, ISeaweedFS seaweedFS) : IMutableFile
{
    public Path Path { get; } = path;
    public ISeaweedFS SeaweedFS { get; } = seaweedFS;

    public string Name => Path.Name();
    public bool IsHidden => false;

    public Task<Result> SetContents(IByteSource data, IScheduler? scheduler = null, CancellationToken cancellationToken = default)
    {
        return SeaweedFS.Upload(Path, data.Bytes.ToStream(), cancellationToken);
    }

    public Task<Result<IByteSource>> GetContents()
    {
        return SeaweedFS.GetFileMetadata(Path)
            .Bind(_ => SeaweedFS.GetFileContents(Path))
            .Map(stream => ByteSource.FromStream(stream));
    }

    public override string ToString()
    {
        return Path;
    }

    public Task<Result<bool>> Exists()
    {
        var exists = SeaweedFS.GetFileMetadata(Path)
            .Match(
                _ => Result.Success(true),
                err =>
                {
                    if (err.Contains("404"))
                    {
                        return Result.Success(false);
                    }

                    return Result.Failure<bool>(err);
                });
        return exists;
    }
}