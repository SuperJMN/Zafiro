using System.Diagnostics;
using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class Path : ValueObject
{
    public const char ChunkSeparator = '/';

    public static readonly Path Empty = new();

    public Path(string path) : this(GetChunks(path))
    {
    }

    public Path(IEnumerable<string> relativePathChunks)
    {
        RouteFragments = relativePathChunks;
    }

    private Path() : this(Enumerable.Empty<string>())
    {
    }

    public IEnumerable<string> RouteFragments { get; }

    public string Value => string.Join(ChunkSeparator, RouteFragments);

    private string DebuggerDisplay => RouteFragments.Any() ? Value : "<root>";

    public static implicit operator Path(string[] chunks)
    {
        return new Path(chunks);
    }

    public static implicit operator Path(string path)
    {
        if (path == "")
        {
            return Empty;
        }

        return new Path(path);
    }

    public static implicit operator string(Path path)
    {
        return path.ToString();
    }

    public static Result<Path> Create(string path)
    {
        if (path.Trim() == "")
        {
            return Result.Failure<Path>("Use Path.Empty to create an empty path (usually root)");
        }

        if (GetChunks(path).Any(string.IsNullOrEmpty))
        {
            return Result.Failure<Path>($"Invalid path {path}");
        }

        return new Path(path);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return Value;
    }

    private static IEnumerable<string> GetChunks(string path)
    {
        return path.Split(ChunkSeparator).ToArray();
    }
}