using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public static class AsyncDirMixin
{
    public static Task<Result<IEnumerable<INamedByteSource>>> Files(this IAsyncDir dir)
    {
        return dir.Children().Map(x => x.OfType<INamedByteSource>());
    }

    public static Task<Result<IEnumerable<INamedContainer>>> Directories(this IAsyncDir dir)
    {
        return dir.Children().Map(x => x.OfType<INamedContainer>());
    }
}