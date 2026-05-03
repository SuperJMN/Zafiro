using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Zafiro.Reactive;

/// <summary>
/// Seekable stream adapter that caches an observable byte sequence on first use.
/// </summary>
/// <remarks>
/// This adapter is intentionally a blocking bridge. <see cref="Length"/> and <see cref="Read(byte[], int, int)"/>
/// wait until the source observable completes. It is only safe at imperative boundaries where no work needed to
/// complete the source is queued on the same thread. Do not use it from inside Rx operators, subscriptions, or
/// UI-thread code; materialize the observable asynchronously and pass a real seekable stream instead. For large
/// payloads, prefer a temporary <see cref="FileStream"/> over an in-memory buffer.
/// </remarks>
public class ResetableObservableStream(IObservable<byte[]> observable) : Stream
{
    private readonly object lockObject = new();
    private byte[]? cachedData;
    private int position;
    private bool hasLoadedData;
    private Exception? loadException;
    private volatile bool isDisposed;

    public override bool CanRead => !isDisposed;
    public override bool CanSeek => !isDisposed;
    public override bool CanWrite => false;

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            EnsureDataLoaded();
            lock (lockObject)
            {
                return cachedData?.Length ?? 0;
            }
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            lock (lockObject)
            {
                return position;
            }
        }
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (offset + count > buffer.Length) throw new ArgumentException("Invalid offset and count");

        ThrowIfDisposed();
        EnsureDataLoaded();

        lock (lockObject)
        {
            if (loadException != null)
            {
                throw loadException;
            }

            if (cachedData == null) return 0;

            int bytesAvailable = cachedData.Length - position;
            int bytesToRead = Math.Min(count, bytesAvailable);

            if (bytesToRead > 0)
            {
                Array.Copy(cachedData, position, buffer, offset, bytesToRead);
                position += bytesToRead;
            }

            return bytesToRead;
        }
    }

    private void EnsureDataLoaded()
    {
        lock (lockObject)
        {
            if (hasLoadedData) return;

            var bufferList = new List<byte>();
            using var waitHandle = new ManualResetEventSlim();
            Exception? exception = null;

            using var subscription = observable.Subscribe(
                onNext: data =>
                {
                    lock (lockObject)
                    {
                        if (!isDisposed)
                        {
                            bufferList.AddRange(data);
                        }
                    }
                },
                onError: ex =>
                {
                    lock (lockObject)
                    {
                        exception = ex;
                    }
                    waitHandle.Set();
                },
                onCompleted: () => waitHandle.Set()
            );

            // Release the lock while waiting to avoid blocking other operations
            Monitor.Exit(lockObject);
            try
            {
                waitHandle.Wait();
            }
            finally
            {
                Monitor.Enter(lockObject);
            }

            if (exception != null)
            {
                loadException = exception;
            }
            else if (!isDisposed)
            {
                cachedData = bufferList.ToArray();
            }

            hasLoadedData = true;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        EnsureDataLoaded();

        lock (lockObject)
        {
            var newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => position + offset,
                SeekOrigin.End => (cachedData?.Length ?? 0) + offset,
                _ => throw new ArgumentException("Invalid seek origin", nameof(origin))
            };

            if (newPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Seek position cannot be negative");

            if (newPosition > (cachedData?.Length ?? 0))
                newPosition = cachedData?.Length ?? 0;

            position = (int)newPosition;
            return position;
        }
    }

    private void ThrowIfDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException(nameof(ResetableObservableStream));
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !isDisposed)
        {
            lock (lockObject)
            {
                isDisposed = true;
                cachedData = null;
            }
        }
        base.Dispose(disposing);
    }
}
