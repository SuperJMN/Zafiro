using CSharpFunctionalExtensions;

namespace Zafiro.UserStorage;

public interface IJsonUserStorage
{
    Task<Result> Save<T>(Path key, T value, CancellationToken cancellationToken = default);
    Task<Result<Maybe<T>>> Load<T>(Path key, CancellationToken cancellationToken = default);
    Task<Result<T>> LoadOrCreate<T>(Path key, Func<T> createDefault, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<StoredValue<T>>>> List<T>(Path prefix, CancellationToken cancellationToken = default);
    Task<Result> Delete(Path key, CancellationToken cancellationToken = default);
}
