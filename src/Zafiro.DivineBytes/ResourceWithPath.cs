using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

public record ResourceWithPath(Path Path, INamedByteSource NamedByteSource) : INamedByteSourceWithPath
{
    public string Name => NamedByteSource.Name;
    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return NamedByteSource.Subscribe(observer);
    }

    public IObservable<byte[]> Bytes => NamedByteSource.Bytes;
    public Maybe<long> Length => NamedByteSource.Length;
}
