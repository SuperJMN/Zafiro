using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Linq;
using CSharpFunctionalExtensions;
using Zafiro.Reactive;

namespace Zafiro.DivineBytes;

/// <summary>
/// A reactive source of byte arrays that also provides an optional asynchronous way to retrieve its length.
/// </summary>
public class ByteSource(IObservable<byte[]> bytes) : IByteSource
{
    private const int DefaultBufferSize = 1_048_576;

    /// <summary>
    /// Exposes the underlying observable of byte[] blocks.
    /// </summary>
    public IObservable<byte[]> Bytes => bytes;

    /// <summary>
    /// Constructor overload that accepts an observable of byte chunks (IEnumerable<byte>) 
    /// and transforms each chunk into a byte array internally.
    /// </summary>
    public ByteSource(IObservable<IEnumerable<byte>> byteChunks) : this(byteChunks.Select(x => x.ToArray()))
    {
    }

    /// <summary>
    /// Creates a ByteSource from a byte array. The data is split into chunks of the specified bufferSize.
    /// The length is automatically taken as bytes.Length.
    /// </summary>
    /// <param name="bytes">Source array of bytes.</param>
    /// <param name="bufferSize">Size of each emitted chunk.</param>
    /// <returns>An IByteSource.</returns>
    public static IByteSource FromBytes(byte[] bytes, int bufferSize = DefaultBufferSize)
    {
        // Avoid per-byte observables and avoid extra ToArray() copies; emit chunks directly
        return FromByteObservable(bytes.Chunk(bufferSize).ToObservable(Scheduler.Immediate));
    }

    /// <summary>
    /// Creates a ByteSource from an observable of byte chunks (IEnumerable<byte>).
    /// </summary>
    /// <param name="byteChunks">Observable sequence where each item is a chunk of bytes (IEnumerable&lt;byte&gt;).</param>
    /// <returns>An IByteSource.</returns>
    public static IByteSource FromByteChunks(IObservable<IEnumerable<byte>> byteChunks)
    {
        return new ByteSource(byteChunks);
    }

    /// <summary>
    /// Creates a ByteSource from an observable of byte arrays.
    /// </summary>
    /// <param name="byteObservable">Observable sequence where each item is an array of bytes.</param>
    /// <returns>An IByteSource.</returns>
    public static IByteSource FromByteObservable(
        IObservable<byte[]> byteObservable)
    {
        return new ByteSource(byteObservable);
    }

    /// <summary>
    /// Creates a ByteSource from a synchronous Stream factory.
    /// The getLength function can provide a length if known.
    /// </summary>
    /// <param name="streamFactory">A factory method that returns a Stream to read from.</param>
    /// <param name="bufferSize">Size of each emitted chunk.</param>
    /// <returns>An IByteSource.</returns>
    public static IByteSource FromStreamFactory(
        Func<Stream> streamFactory,
        int bufferSize = DefaultBufferSize)
    {
        return new ByteSource(Observable.Defer(() => streamFactory().ToObservable(bufferSize)));
    }

    /// <summary>
    /// Creates a ByteSource from a Stream instance.
    /// </summary>
    /// <param name="stream">An existing Stream instance.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after completion.</param>
    /// <param name="bufferSize">Size of each emitted chunk.</param>
    /// <returns>An IByteSource.</returns>
    public static IByteSource FromStream(
        Stream stream,
        int bufferSize = DefaultBufferSize)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        return new ByteSource(stream.ToObservable(bufferSize));
    }

    /// <summary>
    /// Creates a ByteSource from a string using a specified encoding and buffer size.
    /// The getLength function is computed based on the number of bytes for that string.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8 if null.</param>
    /// <param name="bufferSize">Size of each emitted chunk.</param>
    /// <returns>An IByteSource.</returns>
    public static IByteSource FromString(
        string str,
        Encoding? encoding,
        int bufferSize = DefaultBufferSize)
    {
        encoding ??= Encoding.UTF8;

        return new ByteSource(str.ToByteStream(encoding, bufferSize));
    }

    public static IByteSource FromString(
        string str)
    {
        return new ByteSource(str.ToByteStream(Encoding.UTF8));
    }

    /// <summary>
    /// Creates a ByteSource from an asynchronous Stream factory.
    /// The getLength function can provide a length if known.
    /// </summary>
    /// <param name="streamFactory">A factory method that returns a Task of a Stream to read from.</param>
    /// <returns>An IByteSource.</returns>
    public static IByteSource FromAsyncStreamFactory(
        Func<Task<Stream>> streamFactory,
        int bufferSize = DefaultBufferSize)
    {
        return new ByteSource(Observable.FromAsync(streamFactory).SelectMany(stream => stream.ToObservable(bufferSize)));
    }

    /// <summary>
    /// Creates a ByteSource from an async factory that produces a disposable resource,
    /// which is then transformed into a ByteSource. The resource is created lazily on subscription
    /// and disposed automatically when the stream completes or errors.
    /// </summary>
    /// <typeparam name="T">The type of the disposable resource.</typeparam>
    /// <param name="resourceFactory">Async factory that creates the disposable resource wrapped in a Result.</param>
    /// <param name="byteSourceFactory">Function that creates an IByteSource from the resource.</param>
    /// <returns>An IByteSource that manages the resource lifecycle.</returns>
    public static IByteSource FromDisposableAsync<T>(
        Func<Task<Result<T>>> resourceFactory,
        Func<T, IByteSource> byteSourceFactory) where T : IDisposable
    {
        return new ByteSource(Observable.Defer(() =>
            Observable.FromAsync(resourceFactory)
                .SelectMany(result => result.IsSuccess
                    ? byteSourceFactory(result.Value).Bytes.Finally(() => result.Value.Dispose())
                    : Observable.Throw<byte[]>(new InvalidOperationException(result.Error)))));
    }

    /// <summary>
    /// Creates a ByteSource from an async factory that produces a disposable resource,
    /// which is then transformed into a ByteSource. The transform can fail, returning a Result.
    /// The resource is created lazily on subscription and disposed automatically.
    /// </summary>
    /// <typeparam name="T">The type of the disposable resource.</typeparam>
    /// <param name="resourceFactory">Async factory that creates the disposable resource wrapped in a Result.</param>
    /// <param name="transform">Function that transforms the resource into a Result&lt;IByteSource&gt;.</param>
    /// <returns>An IByteSource that manages the resource lifecycle.</returns>
    public static IByteSource FromDisposableAsync<T>(
        Func<Task<Result<T>>> resourceFactory,
        Func<T, Result<IByteSource>> transform) where T : IDisposable
    {
        return FromDisposableAsync(
            resourceFactory,
            resource => transform(resource).Match(
                src => src,
                error => throw new InvalidOperationException(error)));
    }

    /// <summary>
    /// Creates a ByteSource from an async factory that produces a disposable resource,
    /// which is then transformed asynchronously into a ByteSource. The transform can fail.
    /// The resource is created lazily on subscription and disposed automatically.
    /// </summary>
    /// <typeparam name="T">The type of the disposable resource.</typeparam>
    /// <param name="resourceFactory">Async factory that creates the disposable resource wrapped in a Result.</param>
    /// <param name="transformAsync">Async function that transforms the resource into a Result&lt;IByteSource&gt;.</param>
    /// <returns>An IByteSource that manages the resource lifecycle.</returns>
    public static IByteSource FromDisposableAsync<T>(
        Func<Task<Result<T>>> resourceFactory,
        Func<T, Task<Result<IByteSource>>> transformAsync) where T : IDisposable
    {
        return new ByteSource(Observable.Defer(() =>
            Observable.FromAsync(resourceFactory)
                .SelectMany(async result =>
                {
                    if (result.IsFailure)
                    {
                        return Observable.Throw<byte[]>(new InvalidOperationException(result.Error));
                    }

                    var transformResult = await transformAsync(result.Value);
                    return transformResult.Match(
                        src => src.Bytes.Finally(() => result.Value.Dispose()),
                        error =>
                        {
                            result.Value.Dispose();
                            return Observable.Throw<byte[]>(new InvalidOperationException(error));
                        });
                })
                .Switch()));
    }

    /// <summary>
    /// Subscribes to the underlying IObservable of byte arrays.
    /// </summary>
    /// <param name="observer">Observer that will receive the byte arrays.</param>
    /// <returns>A disposable subscription.</returns>
    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return Bytes.Subscribe(observer);
    }
}