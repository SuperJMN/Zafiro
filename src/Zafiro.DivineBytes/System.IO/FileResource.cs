using System.IO.Abstractions;
using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes.System.IO;

internal class FileResource(IFileInfo info) : INamedByteSource
{
    public IByteSource Source { get; } = ByteSource.FromStreamFactory(info.OpenRead, Maybe.From(info.Length));

    public string Name => info.Name;
    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return Source.Subscribe(observer);
    }

    public IObservable<byte[]> Bytes => Source.Bytes;
    public Maybe<long> Length => Source.Length;
}
