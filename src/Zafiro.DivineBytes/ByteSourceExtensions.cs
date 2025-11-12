using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CSharpFunctionalExtensions;
using Zafiro.Reactive;

namespace Zafiro.DivineBytes;

public static class ByteSourceExtensions
{
    public static Stream ToStream(this IByteSource byteSource) => byteSource.Bytes.ToStream();
    public static Stream ToStreamSeekable(this IByteSource byteSource) => byteSource.Bytes.ToStreamSeekable();

    /// <summary>
    /// Chunked write emitting per-chunk results. Default is zero-copy (fast). For a defensive copy per chunk, use WriteToChunkedSafe.
    /// </summary>
    public static IObservable<Result> WriteToChunked(this IByteSource byteSource, Stream destination, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = null, CancellationToken cancellationToken = default)
        => byteSource.Bytes.WriteTo(destination, chunkReadTimeout, scheduler, cancellationToken);

    /// <summary>
    /// Chunked write with defensive copy per chunk (safe for buffer-reusing producers).
    /// </summary>
    public static IObservable<Result> WriteToChunkedSafe(this IByteSource byteSource, Stream destination, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = null, CancellationToken cancellationToken = default)
        => byteSource.Bytes.WriteToSafe(destination, chunkReadTimeout, scheduler, cancellationToken);

    /// <summary>
    /// Final-result write (default is zero-copy): aggregates chunk results into a single Result.
    /// </summary>
    public static Task<Result> WriteTo(this IByteSource byteSource, Stream destination, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = null, CancellationToken cancellationToken = default)
    {
        return byteSource
            .Bytes
            .WriteTo(destination, chunkReadTimeout, scheduler, cancellationToken)
            .ToList()
            .Select(list => list.Combine())
            .ToTask(cancellationToken);
    }

    /// <summary>
    /// Final-result write with defensive copy per chunk.
    /// </summary>
    public static Task<Result> WriteToSafe(this IByteSource byteSource, Stream destination, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = null, CancellationToken cancellationToken = default)
    {
        return byteSource
            .Bytes
            .WriteToSafe(destination, chunkReadTimeout, scheduler, cancellationToken)
            .ToList()
            .Select(list => list.Combine())
            .ToTask(cancellationToken);
    }

    public static async Task<Result> WriteTo(this IByteSource byteSource, string path, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = null, CancellationToken cancellationToken = default)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return await Result.Try(() =>
            {
                var directoryName = global::System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
            })
            .Bind(async () =>
            {
                await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
                return await byteSource.WriteTo(stream, chunkReadTimeout, scheduler, cancellationToken).ConfigureAwait(false);
            })
            .ConfigureAwait(false);
    }

    public static async Task<Result> WriteToSafe(this IByteSource byteSource, string path, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = null, CancellationToken cancellationToken = default)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return await Result.Try(() =>
            {
                var directoryName = global::System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
            })
            .Bind(async () =>
            {
                await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
                return await byteSource.WriteToSafe(stream, chunkReadTimeout, scheduler, cancellationToken).ConfigureAwait(false);
            })
            .ConfigureAwait(false);
    }
}
