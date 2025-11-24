using Zafiro.FileSystem.Mutable;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Zafiro.FileSystem.Local;

public class FileSystem : IMutableFileSystem
{
    public FileSystem(IFileSystem inner)
    {
        Inner = inner;
    }

    public IFileSystem Inner { get; }

    public Task<Result<IMutableDirectory>> GetDirectory(Path path)
    {
        if (OperatingSystem.IsLinux())
        {
            var translatedPath = "/" + path;

            return Task.FromResult(Result
                .Try(() => Inner.DirectoryInfo.New(translatedPath))
                .Map(d => (IMutableDirectory)new Directory(d)));
        }

        if (OperatingSystem.IsWindows())
        {
            if (path == Path.Empty)
            {
                var mutableDirectory = (IMutableDirectory)new WindowsRoot(Inner);
                return Task.FromResult(Result.Success(mutableDirectory));
            }

            return Task.FromResult(Result
                .Try(() => Inner.DirectoryInfo.New(path))
                .Map(d => (IMutableDirectory)new Directory(d)));
        }

        throw new NotSupportedException("Only supported OSes are Windows and Linux for now");
    }

    public async Task<Result<IMutableDirectory>> GetTemporaryDirectory(Path path)
    {
        return Result.Try(() => Inner.Directory.CreateTempSubdirectory())
            .Map(info => (IMutableDirectory)new Directory(info));
    }

    public Path InitialPath
    {
        get
        {
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return OperatingSystem.IsWindows() ? FromWindows(folderPath) : folderPath[1..];
        }
    }

    public Task<Result<IMutableFile>> GetFile(Path path)
    {
        if (OperatingSystem.IsLinux())
        {
            var translatedPath = "/" + path;

            return Task.FromResult(Result
                .Try(() => Inner.FileInfo.New(translatedPath))
                .Map(d => (IMutableFile)new File(d)));
        }

        if (OperatingSystem.IsWindows())
        {
            return Task.FromResult(Result
                .Try(() => Inner.FileInfo.New(path))
                .Map(d => (IMutableFile)new File(d)));
        }

        return Task.FromResult(Result.Failure<IMutableFile>("Not implemented"));
    }

    private Path FromWindows(string folderPath)
    {
        return folderPath.Replace("\\", "/");
    }
}