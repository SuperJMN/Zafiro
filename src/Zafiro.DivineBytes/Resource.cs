using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

public class Resource(string name, IByteSource source) : INamedByteSource
{
    public string Name => name;

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return source.Subscribe(observer);
    }

    public IObservable<byte[]> Bytes => source.Bytes;
    public Maybe<long> Length => source.Length;
}
