using CSharpFunctionalExtensions;

namespace Zafiro.FileSystem;

public sealed class Path : ValueObject
{
    public const char ChuckSeparator = '/';

    public Path(string path)
    {
        Path = path;
    }

    public Path(params string[] routeFragments)
    {
        Path = string.Join(ChuckSeparator, routeFragments);
    }

    public Path(IEnumerable<string> relativePathChunks) : this(relativePathChunks.ToArray())
    {
    }

    public IEnumerable<string> RouteFragments => Path.Split(ChuckSeparator);

    public static implicit operator Path(string[] chunks)
    {
        return new Path(chunks);
    }

    public static implicit operator Path(string path)
    {
        return new Path(path);
    }

    public static implicit operator string(Path path)
    {
        return path.ToString();
    }

    public override string ToString()
    {
        return string.Join(ChuckSeparator, RouteFragments);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Path;
    }

    public string Path { get; }
}