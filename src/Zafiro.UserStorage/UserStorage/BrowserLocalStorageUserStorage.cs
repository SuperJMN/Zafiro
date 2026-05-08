using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using CSharpFunctionalExtensions;

namespace Zafiro.UserStorage;

public sealed partial class BrowserLocalStorageUserStorage : IUserStorage
{
    private const string IndexName = "__zafiro_user_storage_index";
    private readonly string keyPrefix;

    public BrowserLocalStorageUserStorage(string keyPrefix)
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            throw new ArgumentException("Key prefix cannot be empty.", nameof(keyPrefix));
        }

        this.keyPrefix = keyPrefix.Trim();
    }

    public async Task<Result> Save(Path key, IByteSource content, CancellationToken cancellationToken = default)
    {
        return await EnsureBrowser()
            .Bind(() => UserStoragePath.ValidateFileKey(key))
            .Bind(() => content.ReadAll(cancellationToken))
            .Bind(bytes => Result.Try(() =>
            {
                SetItem(ToStorageKey(key.Value), Convert.ToBase64String(bytes));
                var index = LoadIndex();
                index.Add(key.Value);
                SaveIndex(index);
            }));
    }

    public Task<Result<Maybe<IByteSource>>> Load(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EnsureBrowser()
            .Bind(() => UserStoragePath.ValidateFileKey(key))
            .Bind(() => Result.Try<Maybe<IByteSource>>(() =>
            {
                var value = GetItem(ToStorageKey(key.Value));
                if (value is null)
                {
                    return Maybe<IByteSource>.None;
                }

                return Maybe<IByteSource>.From(ByteSource.FromBytes(Convert.FromBase64String(value)));
            })));
    }

    public Task<Result> Delete(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EnsureBrowser()
            .Bind(() => UserStoragePath.ValidateFileKey(key))
            .Bind(() => Result.Try(() =>
            {
                RemoveItem(ToStorageKey(key.Value));
                var index = LoadIndex();
                index.Remove(key.Value);
                SaveIndex(index);
            })));
    }

    public Task<Result<bool>> Exists(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EnsureBrowser()
            .Bind(() => UserStoragePath.ValidateFileKey(key))
            .Bind(() => Result.Try(() => GetItem(ToStorageKey(key.Value)) is not null)));
    }

    public Task<Result<IReadOnlyList<IStoredByteSource>>> List(Path prefix, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EnsureBrowser()
            .Bind(() => UserStoragePath.ValidatePrefix(prefix))
            .Bind(() => Result.Try<IReadOnlyList<IStoredByteSource>>(() =>
            {
                return LoadIndex()
                    .Select(value => new Path(value))
                    .Where(key => UserStoragePath.IsUnderPrefix(key, prefix))
                    .Select(key => (key, value: GetItem(ToStorageKey(key.Value))))
                    .Where(pair => pair.value is not null)
                    .Select(pair => (IStoredByteSource)new StoredByteSource(pair.key, ByteSource.FromBytes(Convert.FromBase64String(pair.value!))))
                    .OrderBy(source => source.Key.Value, StringComparer.Ordinal)
                    .ToArray();
            })));
    }

    private HashSet<string> LoadIndex()
    {
        var json = GetItem(ToStorageKey(IndexName));
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
    }

    private void SaveIndex(HashSet<string> index)
    {
        SetItem(ToStorageKey(IndexName), JsonSerializer.Serialize(index.OrderBy(x => x, StringComparer.Ordinal)));
    }

    private string ToStorageKey(string key)
    {
        return $"{keyPrefix}:{key}";
    }

    private static Result EnsureBrowser()
    {
        return OperatingSystem.IsBrowser()
            ? Result.Success()
            : Result.Failure("BrowserLocalStorageUserStorage can only be used on browser/WASM.");
    }

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? GetItem(string key);

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetItem(string key, string value);

    [JSImport("globalThis.localStorage.removeItem")]
    private static partial void RemoveItem(string key);
}
