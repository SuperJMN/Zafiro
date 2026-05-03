using System.Reactive.Linq;
using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes.Unix;

public class UnixFile : INamedByteSource, IPermissioned, IOwned
{
    private readonly INamedByteSource inner;
    public string Name => inner.Name;
    public UnixPermissions Permissions { get; }

    public UnixFile(INamedByteSource inner, UnixPermissions perms, int ownerId)
    {
        this.inner = inner;
        Permissions = perms;
        OwnerId = ownerId;
    }

    public IObservable<byte[]> Bytes
        => Permissions.OwnerRead
            ? inner.Bytes
            : Observable.Empty<byte[]>();

    public Maybe<long> Length
        => Permissions.OwnerRead
            ? inner.Length
            : Maybe.From(0L);

    public IDisposable Subscribe(IObserver<byte[]> observer)
    {
        return Bytes.Subscribe(observer);
    }

    public int OwnerId { get; }
}
