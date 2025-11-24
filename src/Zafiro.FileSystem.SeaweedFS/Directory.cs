using Zafiro.DivineBytes;
using ClientDirectory = Zafiro.FileSystem.SeaweedFS.Filer.Client.Directory;

namespace Zafiro.FileSystem.SeaweedFS;

public class Directory : DirectoryBase
{
    private Directory(Path path, ISeaweedFS seaweedFS)
    {
        SeaweedFS = seaweedFS;
        Path = path;
    }

    public Path Path { get; }
    public ISeaweedFS SeaweedFS { get; }
    public override string Name => Path.Name();

    public override bool IsHidden => false;

    public static async Task<Result<Directory>> From(string path, ISeaweedFS seaweedFSClient)
    {
        return new Directory(path, seaweedFSClient);
    }

    private IEnumerable<IMutableNode> DirectoryToNodes(RootDirectory directory)
    {
        var entries = directory.Entries ?? new List<BaseEntry>();
        return entries.Select(entry =>
        {
            return entry switch
            {
                ClientDirectory dir => new Directory(dir.FullPath[1..], SeaweedFS),
                FileMetadata file => (IMutableNode)new File(file.FullPath[1..], SeaweedFS),
                _ => throw new ArgumentOutOfRangeException(nameof(entry))
            };
        });
    }

    protected override Task<Result<IMutableFile>> CreateFileCore(string entryName)
    {
        return Task.FromResult<Result<IMutableFile>>(new File(Path.Combine(entryName), SeaweedFS));
    }

    protected override Task<Result> DeleteSubdirectoryCore(string name)
    {
        return SeaweedFS.DeleteDirectory(Path.Combine(name));
    }

    public override Task<Result<IEnumerable<IMutableNode>>> GetChildren(CancellationToken cancellationToken = default)
    {
        return SeaweedFS.GetContents(Path, cancellationToken).Map(DirectoryToNodes);
    }

    public override Task<Result<bool>> HasFile(string name)
    {
        return SeaweedFS.PathExists(Path.Combine(name));
    }

    public override Task<Result<bool>> HasSubdirectory(string name)
    {
        return SeaweedFS.PathExists(Path.Combine(name));
    }

    protected override Task<Result<IMutableDirectory>> CreateSubdirectoryCore(string name)
    {
        var directoryPath = Path.Combine(name);
        return SeaweedFS.CreateFolder(directoryPath).Map(() => (IMutableDirectory)new Directory(directoryPath, SeaweedFS));
    }

    protected override Task<Result> DeleteFileCore(string name)
    {
        return SeaweedFS.DeleteDirectory(Path.Combine(name));
    }

    public override string ToString()
    {
        return Path;
    }
}