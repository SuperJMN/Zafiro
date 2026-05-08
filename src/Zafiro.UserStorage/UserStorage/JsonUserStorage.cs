using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using Zafiro.CSharpFunctionalExtensions;

namespace Zafiro.UserStorage;

public sealed class JsonUserStorage : IJsonUserStorage
{
    private readonly IUserStorage storage;
    private readonly JsonSerializerOptions serializerOptions;

    public JsonUserStorage(IUserStorage storage, JsonSerializerOptions? serializerOptions = null)
    {
        this.storage = storage;
        this.serializerOptions = serializerOptions ?? CreateDefaultOptions();
    }

    public Task<Result> Save<T>(Path key, T value, CancellationToken cancellationToken = default)
    {
        return Result
            .Try(() => JsonSerializer.SerializeToUtf8Bytes(value, serializerOptions))
            .Map(bytes => ByteSource.FromBytes(bytes))
            .Bind(source => storage.Save(key, source, cancellationToken));
    }

    public async Task<Result<Maybe<T>>> Load<T>(Path key, CancellationToken cancellationToken = default)
    {
        return await storage.Load(key, cancellationToken)
            .Bind(source => Deserialize<T>(key, source, cancellationToken));
    }

    public async Task<Result<T>> LoadOrCreate<T>(Path key, Func<T> createDefault, CancellationToken cancellationToken = default)
    {
        return await Load<T>(key, cancellationToken)
            .Bind(maybe => maybe.Match(
                value => Task.FromResult(Result.Success(value)),
                () => SaveDefault(key, createDefault, cancellationToken)));
    }

    public async Task<Result<IReadOnlyList<StoredValue<T>>>> List<T>(Path prefix, CancellationToken cancellationToken = default)
    {
        return await storage.List(prefix, cancellationToken)
            .Bind(async sources =>
            {
                var results = await Task.WhenAll(sources.Select(source => DeserializeStored<T>(source, cancellationToken)));
                return results
                    .Combine()
                    .Map(values => (IReadOnlyList<StoredValue<T>>)values.ToArray());
            });
    }

    public Task<Result> Delete(Path key, CancellationToken cancellationToken = default)
    {
        return storage.Delete(key, cancellationToken);
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private async Task<Result<Maybe<T>>> Deserialize<T>(Path key, IByteSource source, CancellationToken cancellationToken)
    {
        var bytes = await source.ReadAll(cancellationToken);

        return bytes.Bind(value => Result
            .Try(() => JsonSerializer.Deserialize<T>(value, serializerOptions))
            .Ensure(deserialized => deserialized is not null, $"Could not deserialize storage key '{key}' as {typeof(T).Name}.")
            .Map(deserialized => Maybe<T>.From(deserialized!)));
    }

    private async Task<Result<T>> SaveDefault<T>(Path key, Func<T> createDefault, CancellationToken cancellationToken)
    {
        var value = createDefault();
        var save = await Save(key, value, cancellationToken);
        return save.Map(() => value);
    }

    private async Task<Result<StoredValue<T>>> DeserializeStored<T>(IStoredByteSource source, CancellationToken cancellationToken)
    {
        var result = await Deserialize<T>(source.Key, source.Content, cancellationToken);
        return result.Bind(maybe => maybe.Match(
            value => Result.Success(new StoredValue<T>(source.Key, value)),
            () => Result.Failure<StoredValue<T>>($"Could not deserialize storage key '{source.Key}'.")));
    }
}
