namespace Zafiro.FileSystem;

public interface IStorable
{
    Path Path { get; }
    string Name { get; }
    Task<Stream> OpenWrite();
    Task<Stream> OpenRead();
}