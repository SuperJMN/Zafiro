using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;
using Zafiro.CSharpFunctionalExtensions;

using Zafiro.Reactive;

namespace Zafiro.FileSystem.Core;

public static class Mixin
{
    public static Path FullPath(this INamedWithPath item)
    {
        return item.Path.Combine(item.Name);
    }



    public static Stream ToStream(this IByteSource file)
    {
        return file.Bytes.ToStream();
    }
}