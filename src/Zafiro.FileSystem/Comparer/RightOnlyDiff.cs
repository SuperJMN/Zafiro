using Zafiro.DivineBytes;
using Zafiro.FileSystem.Core;

namespace Zafiro.FileSystem.Comparer;

public record RightOnlyDiff : FileDiff
{
    public RightOnlyDiff(INamedByteSourceWithPath right)
    {
        Right = right;
    }

    public INamedByteSourceWithPath Right { get; }
}