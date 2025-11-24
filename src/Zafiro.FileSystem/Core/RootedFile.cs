using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public class RootedFile : IRootedFile
{
    public RootedFile(ZafiroPath path, INamedByteSource file)
    {
        Path = path;
        File = file;
    }

    public INamedByteSource File { get; }
    public ZafiroPath Path { get; }

    public INamedByteSource Value => File;
    public string Name => File.Name;
    public IObservable<byte[]> Bytes => File.Bytes;

    public override string ToString()
    {
        return this.FullPath();
    }

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return File.Subscribe(observer);
    }
}