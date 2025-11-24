using Zafiro.DivineBytes;
using Zafiro.FileSystem.Core;

namespace Zafiro.FileSystem.Comparer;

public record BothDiff : FileDiff
{
    public BothDiff(INamedByteSourceWithPath left, INamedByteSourceWithPath right)
    {
        Left = left;
        Right = right;
    }

    public INamedByteSourceWithPath Left { get; }
    public INamedByteSourceWithPath Right { get; }
}