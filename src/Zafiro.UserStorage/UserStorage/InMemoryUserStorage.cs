using CSharpFunctionalExtensions;

namespace Zafiro.UserStorage;

public sealed class InMemoryUserStorage : IUserStorage
{
    private readonly Dictionary<Path, byte[]> contents = new();
    private readonly object gate = new();

    public async Task<Result> Save(Path key, IByteSource content, CancellationToken cancellationToken = default)
    {
        return await UserStoragePath.ValidateFileKey(key)
            .Bind(() => content.ReadAll(cancellationToken))
            .Tap(value =>
            {
                lock (gate)
                {
                    contents[key] = value.ToArray();
                }
            });
    }

    public Task<Result<Maybe<IByteSource>>> Load(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UserStoragePath.ValidateFileKey(key)
            .Bind(() => Result.Try<Maybe<IByteSource>>(() =>
            {
                lock (gate)
                {
                    return contents.TryGetValue(key, out var bytes)
                        ? Maybe<IByteSource>.From(ByteSource.FromBytes(bytes.ToArray()))
                        : Maybe<IByteSource>.None;
                }
            })));
    }

    public Task<Result> Delete(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UserStoragePath.ValidateFileKey(key)
            .Tap(() =>
            {
                lock (gate)
                {
                    contents.Remove(key);
                }
            }));
    }

    public Task<Result<bool>> Exists(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UserStoragePath.ValidateFileKey(key)
            .Map(() =>
            {
                lock (gate)
                {
                    return contents.ContainsKey(key);
                }
            }));
    }

    public Task<Result<IReadOnlyList<IStoredByteSource>>> List(Path prefix, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UserStoragePath.ValidatePrefix(prefix)
            .Map(() =>
            {
                lock (gate)
                {
                    IReadOnlyList<IStoredByteSource> matches = contents
                        .Where(pair => UserStoragePath.IsUnderPrefix(pair.Key, prefix))
                        .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
                        .Select(pair => (IStoredByteSource)new StoredByteSource(pair.Key, ByteSource.FromBytes(pair.Value.ToArray())))
                        .ToArray();

                    return matches;
                }
            }));
    }
}
