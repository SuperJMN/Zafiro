using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace Zafiro.Reactive;

public static class StreamMixin
{
    /// <summary>
    /// Writes a stream of single bytes by buffering into arrays and delegating to the array-based overload (copying by default).
    /// </summary>
    public static IObservable<Result> WriteTo(this IObservable<byte> source, Stream output,
        CancellationToken cancellationToken = default, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = default,
int bufferSize = 1_048_576)
    {
        scheduler ??= Scheduler.Default;
        chunkReadTimeout ??= TimeSpan.FromDays(1);

        return source
            .Buffer(bufferSize)
            .Select(chunk => chunk.ToArray())
            .WriteTo(output, chunkReadTimeout, scheduler, cancellationToken);
    }

    /// <summary>
    /// Default: zero-copy, high-performance. Requires that the source does NOT mutate or reuse arrays until the write completes.
    /// If in doubt, use <see cref=\"WriteToSafe\"/>.
    /// </summary>
    public static IObservable<Result> WriteTo(this IObservable<byte[]> source, Stream output, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = default, CancellationToken cancellationToken = default)
    {
        scheduler ??= Scheduler.Default;
        chunkReadTimeout ??= TimeSpan.FromDays(1);

        return source
            .Timeout(chunkReadTimeout.Value, scheduler)
            .Select(chunk =>
                Observable.FromAsync(
                    () => Result.Try(() => output.WriteAsync(chunk, 0, chunk.Length, cancellationToken)),
                    scheduler))
            .Concat()
            .Catch((TimeoutException te) => Observable.Return(Result.Failure("Timeout reading from source.")))
            .DefaultIfEmpty(Result.Success());
    }

    /// <summary>
    /// Safe variant: performs a per-chunk copy to prevent data corruption when the producer reuses buffers.
    /// </summary>
    public static IObservable<Result> WriteToSafe(this IObservable<byte[]> source, Stream output, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = default, CancellationToken cancellationToken = default)
    {
        scheduler ??= Scheduler.Default;
        chunkReadTimeout ??= TimeSpan.FromDays(1);

        return source
            .Timeout(chunkReadTimeout.Value, scheduler)
            .Select(chunk =>
            {
                var copy = chunk.ToArray();
                return Observable.FromAsync(
                        () => Result.Try(() => output.WriteAsync(copy, 0, copy.Length, cancellationToken)),
                        scheduler);
            })
            .Concat()
            .Catch((TimeoutException te) => Observable.Return(Result.Failure("Timeout reading from source.")))
            .DefaultIfEmpty(Result.Success());
    }

    /// <summary>
    /// Alias for zero-copy default.
    /// </summary>
    public static IObservable<Result> WriteToZeroCopy(this IObservable<byte[]> source, Stream output, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = default, CancellationToken cancellationToken = default)
        => source.WriteTo(output, chunkReadTimeout, scheduler, cancellationToken);

    public static async Task<string> ReadToEnd(this Stream stream, Encoding? encoding = null)
    {
        using var reader = new StreamReader(stream, encoding ?? Encoding.Default);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    public static async Task<byte[]> ReadBytesToEnd(this Stream stream, int bufferSize = 4096, CancellationToken ct = default)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var buffer = new byte[bufferSize];
        int bytesRead;
        var allBytes = new List<byte>();
        do
        {
            bytesRead = await stream.ReadAsync(buffer, 0, bufferSize, ct).ConfigureAwait(false);
            if (bytesRead > 0)
            {
                allBytes.AddRange(buffer.Take(bytesRead));
            }
        } while (bytesRead > 0);

        return allBytes.ToArray();
    }
}
