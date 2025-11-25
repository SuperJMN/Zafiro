using DynamicData;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Dynamic;

public static class DynamicMixin
{
    public static IObservable<IChangeSet<INamedByteSource, string>> AllFiles(this IDynamicDirectory directory)
    {
        return new[] { directory.Files, directory.AllDirectories().MergeManyChangeSets(x => x.AllFiles()) }.MergeChangeSets();
    }

    public static IObservable<IChangeSet<IDynamicDirectory, string>> AllDirectories(this IDynamicDirectory directory)
    {
        return new[] { directory.Directories, directory.Directories.MergeManyChangeSets(x => x.AllDirectories()) }.MergeChangeSets();
    }
}