using CSharpFunctionalExtensions;
using Zafiro.Core;

namespace Zafiro.FileSystem.Smart;

public class CopyOperationMetadata : ValueObject
{
    public CopyOperationMetadata(Host host, Path source, Path destination, Hash hash)
    {
        Host = host;
        Source = source;
        Destination = destination;
        Hash = hash;
    }

    public Host Host { get; }
    public Path Source { get; }
    public Path Destination { get; }
    public Hash Hash { get; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Host;
        yield return Source;
        yield return Destination;
        yield return Hash;
    }
}