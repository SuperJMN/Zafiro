namespace Zafiro.DivineBytes;

public class NamedByteSource : INamedByteSource
{
    public NamedByteSource(string name, IByteSource source)
    {
        Name = name;
        Source = source;
    }

    public string Name { get; }
    public IByteSource Source { get; }
    public IObservable<byte[]> Bytes => Source.Bytes;

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return Source.Subscribe(observer);
    }
}
