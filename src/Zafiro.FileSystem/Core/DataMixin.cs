using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public static class DataMixin
{
    /// <summary>
    /// Reads the entire IByteSource contents as a byte array.
    /// Returns Result to handle errors functionally instead of throwing.
    /// </summary>
    [Obsolete("Use ReadAll() directly on IByteSource instead")]
    public static Task<Result<byte[]>> Bytes(this IByteSource data, CancellationToken cancellationToken = default)
    {
        return data.ReadAll(cancellationToken);
    }

    /// <summary>
    /// Helper to read file contents as bytes in a single operation.
    /// Composes GetContents + ReadAll for ergonomic total error handling.
    /// </summary>
    public static Task<Result<byte[]>> GetContentsBytes(
        this Task<Result<IByteSource>> contentsResult,
        CancellationToken cancellationToken = default)
    {
        return contentsResult.Bind(source => source.ReadAll(cancellationToken));
    }
}
