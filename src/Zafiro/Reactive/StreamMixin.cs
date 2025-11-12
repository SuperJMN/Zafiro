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
    public static IObservable<Result> WriteTo(this IObservable<byte> source, Stream output,
        CancellationToken cancellationToken = default, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = default,
        int bufferSize = 4096)
    {
        scheduler ??= Scheduler.Default;
        chunkReadTimeout ??= TimeSpan.FromDays(1);

        return source
            .Buffer(bufferSize)
            .Select(chunk => chunk.ToArray())
            .WriteTo(output, chunkReadTimeout, scheduler, cancellationToken);
    }

    public static IObservable<Result> WriteTo(this IObservable<byte[]> source, Stream output, TimeSpan? chunkReadTimeout = default, IScheduler? scheduler = default, CancellationToken cancellationToken = default)
    {
        scheduler ??= Scheduler.Default;
        chunkReadTimeout ??= TimeSpan.FromDays(1);

        return source
            .Select(chunk =>
                Observable.FromAsync(
                    () => Result.Try(() => output.WriteAsync(chunk, 0, chunk.Length, cancellationToken)),
                    scheduler)
                .Timeout(chunkReadTimeout.Value, scheduler))
            .Concat()
            .DefaultIfEmpty(Result.Success());
    }

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