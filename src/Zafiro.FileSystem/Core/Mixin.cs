using CSharpFunctionalExtensions;
using Zafiro.DivineBytes;
using Zafiro.CSharpFunctionalExtensions;
using Zafiro.FileSystem.Mutable;
using Zafiro.Reactive;

namespace Zafiro.FileSystem.Core;

public static class Mixin
{
    public static ZafiroPath FullPath<T>(this IRooted<T> rootedFile) where T : INamed
    {
        return rootedFile.Path.Combine(rootedFile.Value.Name);
    }

    public static Task<Result<INamedContainer>> ToDirectory(this IMutableDirectory directory)
    {
        var files = directory
            .Files()
            .Map(files => files.Select(f => f.AsReadOnly()))
            .CombineSequentially();

        var subDirs = directory
            .Directories()
            .Map(dirs => dirs.Select(f => f.ToDirectory()))
            .CombineSequentially();

        return from file in files
               from subdir in subDirs
               select (INamedContainer)new NamedContainer(directory.Name, file, subdir);
    }

    public static Stream ToStream(this IByteSource file)
    {
        return file.Bytes.ToStream();
    }
}