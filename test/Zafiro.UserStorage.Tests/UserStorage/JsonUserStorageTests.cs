using Zafiro.UserStorage;
using Zafiro.Settings;

namespace Zafiro.UserStorage.Tests;

public class JsonUserStorageTests
{
    private sealed record Preferences(bool IsDarkThemeEnabled, int Volume);

    [Fact]
    public async Task LoadOrCreate_persists_default_when_key_is_missing()
    {
        var storage = new JsonUserStorage(new InMemoryUserStorage());

        var result = await storage.LoadOrCreate("settings/ui", () => new Preferences(true, 7));
        var reloaded = await storage.Load<Preferences>("settings/ui");

        Assert.True(result.IsSuccess);
        Assert.Equal(new Preferences(true, 7), result.Value);
        Assert.True(reloaded.IsSuccess);
        Assert.Equal(new Preferences(true, 7), reloaded.Value.Value);
    }

    [Fact]
    public async Task Load_returns_failure_when_json_is_invalid()
    {
        var bytes = new InMemoryUserStorage();
        var storage = new JsonUserStorage(bytes);
        await bytes.Save("settings/ui", ByteSource.FromString("{ not-json"));

        var result = await storage.Load<Preferences>("settings/ui");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task List_returns_typed_values_with_their_logical_keys()
    {
        var storage = new JsonUserStorage(new InMemoryUserStorage());
        await storage.Save("settings/one", new Preferences(true, 1));
        await storage.Save("settings/two", new Preferences(false, 2));
        await storage.Save("other/value", new Preferences(false, 3));

        var result = await storage.List<Preferences>("settings");

        Assert.True(result.IsSuccess);
        Assert.Equal(["settings/one", "settings/two"], result.Value.Select(value => value.Key.Value));
        Assert.Equal([1, 2], result.Value.Select(value => value.Value.Volume));
    }

    [Fact]
    public void UserStorageSettingsStore_adapts_existing_settings_api_to_key_storage()
    {
        var bytes = new InMemoryUserStorage();
        var settingsStore = new UserStorageSettingsStore(new JsonUserStorage(bytes));
        using var settings = new JsonSettings<Preferences>("settings/ui", settingsStore, () => new Preferences(false, 1));

        var initial = settings.Get();
        Assert.True(initial.IsSuccess);
        Assert.Equal(new Preferences(false, 1), initial.Value);
        Assert.True(settings.Update(current => current with { Volume = 9 }).IsSuccess);

        var updated = settings.Get();
        Assert.True(updated.IsSuccess);
        Assert.Equal(new Preferences(false, 9), updated.Value);
    }
}
