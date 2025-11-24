using CSharpFunctionalExtensions;
using Zafiro.FileSystem.Core;

namespace Zafiro.FileSystem.Mutable;

public interface IMutableFileSystem
{
    Path InitialPath { get; }
    Task<Result<IMutableDirectory>> GetDirectory(Path path);
    Task<Result<IMutableDirectory>> GetTemporaryDirectory(Path path);
}