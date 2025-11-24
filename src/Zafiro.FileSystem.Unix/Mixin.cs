using Zafiro.FileSystem.Core;
using Zafiro.DivineBytes;
using Zafiro.Mixins;

namespace Zafiro.FileSystem.Unix;

public static class Mixin
{
    public static TDir ToRoot<TDir>(this ICollection<IRootedFile> allFiles, ZafiroPath parent, Func<string, IEnumerable<INode>, TDir> createDir) where TDir : INamedContainer
    {
        var files = allFiles.Where(x => x.Path == parent).Select(x => (INode)x.Value);
        var allParents = allFiles.Select(x => x.Path.ParentsAndSelf()).Flatten();

        var nextLevelPaths = allParents
            .Where(x => x.RouteFragments.Count() == parent.RouteFragments.Count() + 1)
            .Select(x => x.Path).ToList().Distinct();

        var dirs = nextLevelPaths.Select(x => ToRoot(allFiles, x, createDir)).ToList();
        var unixNodes = files.Concat(dirs.Cast<INode>());
        return createDir(parent.Name(), unixNodes);
    }
}