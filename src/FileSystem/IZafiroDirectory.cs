using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem;

public interface IZafiroDirectory
{
    IEnumerable<IZafiroFile> Files { get; }
    IEnumerable<IZafiroDirectory> Directories { get; }
    Path Path { get; }
    IZafiroFileSystem FileSystem { get; }
    Result<IZafiroFile> GetFile(string name);
}