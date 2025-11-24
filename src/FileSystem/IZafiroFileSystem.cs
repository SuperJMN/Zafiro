using CSharpFunctionalExtensions;
using Serilog;

namespace Zafiro.FileSystem;

public interface IZafiroFileSystem
{
    Result<IZafiroFile> GetFile(Path path);
    Result<IZafiroDirectory> GetDirectory(Path path);
    Maybe<ILogger> Logger { get; }
}