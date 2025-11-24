using System.IO.Abstractions;
using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem;

public class ZafiroFile : IZafiroFile
{
    private readonly IFileInfo fileInfo;

    public ZafiroFile(IFileInfo fileInfo, IZafiroFileSystem zafiroFileSystem)
    {
        this.fileInfo = fileInfo;
        FileSystem = zafiroFileSystem;
    }

    public Path Path => new(fileInfo.FullName.Split(fileInfo.FileSystem.Path.DirectorySeparatorChar));
    public string Name => fileInfo.Name;

    public async Task<Result> CopyTo(IZafiroFile destination)
    {
        return await Result.Try(async () =>
        {
            await using var originStream = fileInfo.OpenRead();
            await using var destinationStream = await destination.OpenWrite();
            await originStream.CopyToAsync(destinationStream).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task<Stream> OpenWrite()
    {
        return Task.FromResult(fileInfo.OpenWrite());
    }

    public Task<Stream> OpenRead()
    {
        return Task.FromResult(fileInfo.OpenRead());
    }

    public IZafiroFileSystem FileSystem { get; }

    public Result Delete()
    {
        return Result
            .Try(() => fileInfo.Delete())
            .OnSuccessTry(() => FileSystem.Logger.Execute(l => l.Verbose("Deleted '{Me}'", this)));
    }

    public override string ToString()
    {
        return Path.ToString();
    }
}