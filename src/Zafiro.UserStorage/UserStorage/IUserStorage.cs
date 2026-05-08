using CSharpFunctionalExtensions;

namespace Zafiro.UserStorage;

public interface IUserStorage
{
    Task<Result> Save(Path key, IByteSource content, CancellationToken cancellationToken = default);
    Task<Result<Maybe<IByteSource>>> Load(Path key, CancellationToken cancellationToken = default);
    Task<Result> Delete(Path key, CancellationToken cancellationToken = default);
    Task<Result<bool>> Exists(Path key, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<IStoredByteSource>>> List(Path prefix, CancellationToken cancellationToken = default);
}
