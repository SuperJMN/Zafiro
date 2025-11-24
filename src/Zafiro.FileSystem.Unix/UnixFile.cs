using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Unix;

public class UnixFile : UnixNode, INamedByteSource
{
    public UnixFile(INamedByteSource file, UnixFileProperties properties) : this(file.Name, file, -1, properties)
    {
    }

    public UnixFile(string name, IByteSource source, long length) : this(name, source, length, Maybe<UnixFileProperties>.None)
    {
    }

    public UnixFile(string name, IByteSource source, long length, Maybe<UnixFileProperties> properties) : base(name)
    {
        Source = source;
        Length = length;
        Properties = properties.GetValueOrDefault(UnixFileProperties.RegularFileProperties);
    }

    public UnixFile(string name) : this(name, ByteSource.FromBytes(Array.Empty<byte>()), 0, Maybe<UnixFileProperties>.None)
    {
    }

    public IByteSource Source { get; }
    public UnixFileProperties Properties { get; }
    public IObservable<byte[]> Bytes => Source.Bytes;
    public long Length { get; }

    public override string ToString() => Name;

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return Source.Subscribe(observer);
    }
}