using CSharpFunctionalExtensions;
using Zafiro.Settings;

namespace Zafiro.UserStorage;

public sealed class UserStorageSettingsStore : ISettingsStore
{
    private readonly IJsonUserStorage storage;

    public UserStorageSettingsStore(IJsonUserStorage storage)
    {
        this.storage = storage;
    }

    public Result<T> Load<T>(string path, Func<T> createDefault)
    {
        return Path.Create(path)
            .Bind(key => storage.LoadOrCreate(key, createDefault).GetAwaiter().GetResult());
    }

    public Result Save<T>(string path, T instance)
    {
        return Path.Create(path)
            .Bind(key => storage.Save(key, instance).GetAwaiter().GetResult());
    }
}
