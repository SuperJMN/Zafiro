using Zafiro.FileSystem.Readonly;

namespace Zafiro.FileSystem.Core;

public class RootedFile : IRootedFile
{
    public RootedFile(ZafiroPath path, IFile file)
    {
        Path = path;
        File = file;
    }

    public IFile File { get; }
    public ZafiroPath Path { get; }

    public IFile Value => File;
    public string Name => File.Name;
    public IObservable<byte[]> Bytes => File.Bytes;
    public long Length => File.Length;

    public override string ToString()
    {
        return this.FullPath();
    }

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return File.Subscribe(observer);
    }
}