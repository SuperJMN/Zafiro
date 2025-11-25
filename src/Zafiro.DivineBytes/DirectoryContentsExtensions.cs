namespace Zafiro.DivineBytes;

public static class DirectoryContentsExtensions
{
    public static IEnumerable<INamedByteSource> ResourcesRecursive(this IContainer container)
        => container.Resources.Concat(container.Subcontainers.SelectMany(d => d.ResourcesRecursive()));

    public static IEnumerable<INamedByteSourceWithPath> ResourcesWithPathsRecursive(this IContainer container, Path? path = null)
    {
        path ??= Path.Empty;
        // Include files as INamedByteSourceWithPath (which implements INamedWithPath)
        var myResources = container.Resources.Select(file => new ResourceWithPath(path, file));

        // Include subdirectories as INamedWithPath and recursively their children
        var subcontainerResults = container.Subcontainers.SelectMany(subcontainer =>
        {
            var subcontainerPath = path.Combine(subcontainer.Name);
            // Return the subdirectory itself plus its children
            return subcontainer.ResourcesWithPathsRecursive(subcontainerPath);
        });

        return myResources.Concat(subcontainerResults);
    }
}

// Helper class to represent a container as INamedWithPath
internal record ContainerWithPath(Path Path, string Name) : INamedWithPath;
