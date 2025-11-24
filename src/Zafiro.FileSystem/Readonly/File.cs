using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Readonly;

public class File : IFile
{
    private readonly IByteSource source;

    public File(string name, IByteSource source, long length)
    {
        this.source = source;
        Name = name;
        Length = length;
    }

    public string Name { get; }
    public IObservable<byte[]> Bytes => source.Bytes;
    public long Length { get; }

    public override string ToString()
    {
        return Name;
    }

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return source.Subscribe(observer);
    }
}