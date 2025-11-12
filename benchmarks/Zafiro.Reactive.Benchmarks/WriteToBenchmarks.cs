using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;
using Zafiro.Reactive;

namespace Zafiro.Reactive.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class WriteToBenchmarks
{
    [Params(100)]
    public int TotalSizeMB { get; set; }

    [Params(65536, 1048576)] // 64KB, 1MB
    public int ChunkSize { get; set; }

    [Params("ByteSource", "Observable")] // how we build the source
    public string SourceKind { get; set; } = "ByteSource";

    [Params(false)]
    public bool UseNullScheduler { get; set; }

    private byte[] data = Array.Empty<byte>();
    private List<byte[]> preSplit = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var totalBytes = TotalSizeMB * 1024L * 1024L;
        data = new byte[totalBytes];
        for (long i = 0; i < totalBytes; i++)
        {
            data[i] = (byte)(i % 251); // deterministic pattern
        }

        preSplit = new List<byte[]>((int)Math.Ceiling(totalBytes / (double)ChunkSize));
        long offset = 0;
        while (offset < totalBytes)
        {
            var len = (int)Math.Min(ChunkSize, totalBytes - offset);
            var chunk = new byte[len];
            Buffer.BlockCopy(data, (int)offset, chunk, 0, len);
            preSplit.Add(chunk);
            offset += len;
        }
    }

    private IObservable<byte[]> BuildObservable()
    {
        // Cold observable that pushes the pre-split chunks
        return Observable.Create<byte[]>(obs =>
        {
            foreach (var chunk in preSplit)
            {
                obs.OnNext(chunk);
            }
            obs.OnCompleted();
            return () => { };
        });
    }

    private IByteSource BuildByteSource()
    {
        if (SourceKind == "ByteSource")
        {
            // Exercise the public API that most callers use
            return ByteSource.FromBytes(data, bufferSize: ChunkSize);
        }
        else
        {
            // Wrap an explicit IObservable<byte[]> to isolate the WriteTo path
            return ByteSource.FromByteObservable(BuildObservable());
        }
    }

    private static IScheduler? ResolveScheduler(bool useNull)
        => useNull ? null : Scheduler.Default;

    [Benchmark(Description = "IByteSource.WriteTo (Task<Result>)")]
    public async Task<Result> IByteSource_WriteTo()
    {
        var src = BuildByteSource();
        using var ms = new MemoryStream(capacity: data.Length);
        return await src.WriteTo(ms, scheduler: ResolveScheduler(UseNullScheduler));
    }

    [Benchmark(Description = "IObservable<byte[]>.WriteTo + Combine (final Result)")]
    public async Task<Result> Observable_WriteTo_FinalResult()
    {
        var src = BuildObservable();
        using var ms = new MemoryStream(capacity: data.Length);
        var results = await src.WriteTo(ms, scheduler: ResolveScheduler(UseNullScheduler))
                               .ToList()
                               .ToTask();
        return results.Combine();
    }

    [Benchmark(Baseline = true, Description = "Baseline direct WriteAsync loop")]
    public async Task Baseline_DirectWriteAsync()
    {
        using var ms = new MemoryStream(capacity: data.Length);
        foreach (var chunk in preSplit)
        {
            await ms.WriteAsync(chunk, 0, chunk.Length);
        }
    }
}
