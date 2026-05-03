using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using CSharpFunctionalExtensions;
using Zafiro.Reactive;

namespace Zafiro.DivineBytes;

public static class ByteSourceExtensions
{
    public static Stream ToStream(this IByteSource byteSource) => byteSource.Bytes.ToStream();

    /// <summary>
    /// Creates a seekable <see cref="Stream"/> by blocking until the complete observable has been materialized.
    /// </summary>
    /// <remarks>
    /// This is a synchronous bridge for APIs that require a seekable stream and inspect <see cref="Stream.Length"/>.
    /// It must not be called from inside an Rx subscription/operator, <c>CurrentThreadScheduler</c> work item,
    /// UI thread, or any code path already executing as part of the same observable pipeline. Doing so can deadlock
    /// when the source is scheduled on the current thread. Prefer <c>WriteTo</c> at an imperative boundary
    /// and pass a real seekable stream, such as a temporary <see cref="FileStream"/>, to synchronous APIs.
    /// Use <see cref="ReadAll"/> only when the payload is known to be small or the target API requires
    /// <c>byte[]</c>.
    /// </remarks>
    public static Stream ToStreamSeekable(this IByteSource byteSource) => byteSource.Bytes.ToStreamSeekable();

    /// <summary>
    /// Reads the entire IByteSource into memory as a single byte array.
    /// Any error in the underlying observable (I/O, decoding, pipeline)
    /// is captured and returned as a failed Result instead of throwing.
    /// </summary>
    /// <remarks>
    /// This buffers the complete source in memory. Use it only for payloads that are known to be small or when a
    /// target API really requires <c>byte[]</c>. For large payloads, stream to the destination or materialize to a
    /// temporary file at the boundary that requires seeking.
    /// </remarks>
    public static async Task<Result<byte[]>> ReadAll(
        this IByteSource source,
        CancellationToken cancellationToken = default)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return await Result.Try(async () =>
        {
            var data = await source.Bytes
                .SelectMany(chunk => chunk)
                .ToArray()
                .ToTask(cancellationToken)
                .ConfigureAwait(false);

            return data;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the entire IByteSource and decodes it as text using the given encoding.
    /// Any error in the underlying observable (I/O, decoding, pipeline)
    /// is captured and returned as a failed Result instead of throwing.
    /// </summary>
    /// <remarks>
    /// This buffers the complete source in memory before decoding. Use it only for text payloads that are known to
    /// be small.
    /// </remarks>
    public static async Task<Result<string>> ReadAllText(
        this IByteSource source,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        encoding ??= Encoding.UTF8;

        return await Result.Try(async () =>
        {
            var bytes = await source.Bytes
                .SelectMany(chunk => chunk)
                .ToArray()
                .ToTask(cancellationToken)
                .ConfigureAwait(false);

            return encoding.GetString(bytes);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Wraps the byte chunks of the source in Result&lt;byte[]&gt; so that errors
    /// are surfaced as a single failed Result item instead of OnError.
    /// Useful when you want streaming with functional error handling.
    /// </summary>
    public static IObservable<Result<byte[]>> ToResultSequence(this IByteSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.Bytes
            .Select(chunk => Result.Success(chunk))
            .Catch<Result<byte[]>, Exception>(ex =>
                Observable.Return(Result.Failure<byte[]>(ex.Message)));
    }

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
