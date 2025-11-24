namespace Zafiro.FileSystem.SeaweedFS;

public class FileSystem : IMutableFileSystem
{
    public FileSystem(ISeaweedFS seaweedFS)
    {
        SeaweedFS = seaweedFS;
    }

    public ISeaweedFS SeaweedFS { get; }

    public Task<Result<IMutableDirectory>> GetDirectory(Path path)
    {
        return Directory.From(path, SeaweedFS).Map(IMutableDirectory (s) => s);
    }

    public Task<Result<IMutableDirectory>> GetTemporaryDirectory(Path path)
    {
        throw new NotImplementedException();
    }

    public Path InitialPath => Path.Empty;
}