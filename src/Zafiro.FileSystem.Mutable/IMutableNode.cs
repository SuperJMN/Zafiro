using Zafiro.FileSystem.Core;

namespace Zafiro.FileSystem.Mutable;

public interface IMutableNode : INode
{
    public bool IsHidden { get; }
}