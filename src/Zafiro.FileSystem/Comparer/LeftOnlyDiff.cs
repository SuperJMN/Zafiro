using Zafiro.DivineBytes;
using Zafiro.FileSystem.Core;

namespace Zafiro.FileSystem.Comparer;

public record LeftOnlyDiff : FileDiff
{
    public LeftOnlyDiff(INamedByteSourceWithPath left)
    {
        Left = left;
    }

    public INamedByteSourceWithPath Left { get; }
}