using System.IO;
using IOPath = System.IO.Path;
using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

public static class ByteSourceDetacher
{
    public static async Task<Result<IByteSource>> Detach(IByteSource source, string? fileName)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var safeName = string.IsNullOrWhiteSpace(fileName) ? "detached" : fileName;
        var uniqueName = $"{IOPath.GetFileNameWithoutExtension(safeName)}-{Guid.NewGuid():N}{IOPath.GetExtension(safeName)}";
        var tempPath = IOPath.Combine(IOPath.GetTempPath(), uniqueName);

        var writeResult = await source.WriteTo(tempPath);
        if (writeResult.IsFailure)
        {
            return writeResult.ConvertFailure<IByteSource>();
        }

        var streamOptions = new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.DeleteOnClose
        };

        return Result.Success<IByteSource>(ByteSource.FromAsyncStreamFactory(
            () => Task.FromResult<Stream>(new FileStream(tempPath, streamOptions))));
    }
}
