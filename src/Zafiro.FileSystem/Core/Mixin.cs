using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public static class Mixin
{
    public static Path FullPath(this INamedWithPath item)
    {
        return item.Path.Combine(item.Name);
    }
}
