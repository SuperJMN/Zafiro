using IOPath = System.IO.Path;
using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

/// <summary>
/// Provides functionality to detach data from an <see cref="IByteSource"/> and save it as a temporary file.
/// The detached data is written to a uniquely named temporary file, which is automatically cleaned up when the
/// file's associated stream is closed. This utility is particularly useful in scenarios where byte data needs to
/// be temporarily offloaded into the filesystem for further processing or storage.
/// </summary>
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

        return Result.Success(ByteSource.FromAsyncStreamFactory(
            () => Task.FromResult<Stream>(new FileStream(tempPath, streamOptions))));
    }
}
