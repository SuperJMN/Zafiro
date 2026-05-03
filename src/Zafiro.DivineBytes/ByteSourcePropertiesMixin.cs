using System.Reactive.Linq;
using System.Security.Cryptography;
using Zafiro.Mixins;
using Zafiro.Reactive;
using Crc32 = System.IO.Hashing.Crc32;

namespace Zafiro.DivineBytes;

public static class ByteSourcePropertiesMixin
{
    /// <summary>
    /// Synchronously flattens the complete observable into a byte array.
    /// </summary>
    /// <remarks>
    /// This method blocks while enumerating the observable. Do not call it from inside Rx operators,
    /// subscriptions, <c>CurrentThreadScheduler</c> work, or UI-thread paths. Prefer asynchronous
    /// materialization at an imperative boundary. For large payloads, stream to a file instead of collecting
    /// the whole sequence in memory.
    /// </remarks>
    public static byte[] Array(this IObservable<byte[]> data)
    {
        return data.ToEnumerable().Flatten().ToArray();
    }

    /// <summary>
    /// Emits the total number of bytes produced by the observable.
    /// </summary>
    /// <remarks>
    /// The returned observable is lazy and should be subscribed or awaited asynchronously. Blocking it with
    /// <c>Wait()</c>, <c>Result</c>, or <c>GetAwaiter().GetResult()</c> from inside the same Rx pipeline can
    /// deadlock when the source is scheduled on the current thread.
    /// </remarks>
    public static IObservable<long> GetSize(this IObservable<byte[]> data)
    {
        return data.Sum(bytes => (long)bytes.Length);
    }

    public static IObservable<uint> Crc32(this IObservable<byte[]> data)
    {
        return data.Aggregate(
                new Crc32(),
                (crc, chunk) =>
                {
                    crc.Append(chunk);
                    return crc;
                })
            .Select(crc => crc.GetCurrentHashAsUInt32());
    }

    public static IObservable<byte[]> Sha256(this IObservable<byte[]> data)
    {
        return data.Aggregate(
                new Sha256Accumulator(),
                (acc, chunk) => acc.Append(chunk)
            )
            .Select(acc => acc.GetHash());
    }

    public static IObservable<uint> Crc32(this IByteSource byteSource)
    {
        return byteSource.Bytes.Crc32();
    }

    public static IObservable<long> GetSize(this IByteSource byteSource)
    {
        return byteSource.Length.HasValue
            ? Observable.Return(byteSource.Length.Value)
            : byteSource.Bytes.GetSize();
    }

    /// <summary>
    /// Synchronously flattens the complete byte source into a byte array.
    /// </summary>
    /// <remarks>
    /// This method blocks while enumerating the source. Do not call it from inside Rx operators,
    /// subscriptions, <c>CurrentThreadScheduler</c> work, or UI-thread paths. For large payloads, prefer
    /// streaming to a file instead of collecting the whole source in memory.
    /// </remarks>
    public static byte[] Array(this IByteSource byteSource)
    {
        return byteSource.Bytes.Array();
    }

    public static IObservable<byte[]> Sha256(this IByteSource byteSource)
    {
        return byteSource.Bytes.Sha256();
    }

    public class Sha256Accumulator
    {
        private readonly SHA256 sha256;
        private bool finished;

        public Sha256Accumulator()
        {
            sha256 = SHA256.Create();
            finished = false;
        }

        public Sha256Accumulator Append(byte[] chunk)
        {
            if (finished)
                throw new InvalidOperationException("No se pueden agregar datos una vez finalizado el hash.");

            sha256.TransformBlock(chunk, 0, chunk.Length, chunk, 0);
            return this;
        }

        public byte[] GetHash()
        {
            if (!finished)
            {
                // Finalizamos el procesamiento (es como decirle al SHA256: "¡Se acabó la función!")
                sha256.TransformFinalBlock([], 0, 0);
                finished = true;
            }
            return sha256.Hash ?? throw new InvalidOperationException();
        }
    }
}
