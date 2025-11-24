using Zafiro.DivineBytes;
using Zafiro.FileSystem.Core;

namespace Zafiro.FileSystem.Readonly;

public interface IFile : INamedByteSource, INode
{
    long Length { get; }
}