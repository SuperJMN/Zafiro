using System.Reactive.Linq;
using System.Text;
using System.Linq;
using Zafiro.Mixins;

namespace Zafiro.DivineBytes;

public static class ByteStreamExtensions
{
    public static IObservable<IEnumerable<byte>> ToByteStream(this IEnumerable<byte> bytes, int bufferSize = 4096)
    {
        // Fast path: emit one OnNext per chunk instead of one per byte.
        // Chunk() yields T[]; we upcast to IEnumerable<byte> to preserve the public API.
        return bytes
            .Chunk(bufferSize)
            .Select(chunk => (IEnumerable<byte>)chunk)
            .ToObservable();
    }

    public static IObservable<IEnumerable<byte>> ToByteStream(this string text, Encoding? encoding, int bufferSize = 4096)
    {
        return text.ToBytes(encoding ?? Encoding.UTF8).ToByteStream(bufferSize);
    }
}
