using Zafiro.UserStorage;

namespace Zafiro.UserStorage.Tests;

public class InMemoryUserStorageTests
{
    [Fact]
    public async Task Save_load_exists_delete_roundtrips_by_logical_key()
    {
        var storage = new InMemoryUserStorage();
        var key = (Path)"settings/graphics";

        var save = await storage.Save(key, ByteSource.FromString("enabled"));
        Assert.True(save.IsSuccess);

        var exists = await storage.Exists(key);
        Assert.True(exists.IsSuccess);
        Assert.True(exists.Value);
        var loaded = await storage.Load(key);
        Assert.True(loaded.IsSuccess);
        Assert.True(loaded.Value.HasValue);
        var text = await loaded.Value.Value.ReadAllText();
        Assert.True(text.IsSuccess);
        Assert.Equal("enabled", text.Value);

        var delete = await storage.Delete(key);
        Assert.True(delete.IsSuccess);
        var existsAfterDelete = await storage.Exists(key);
        Assert.True(existsAfterDelete.IsSuccess);
        Assert.False(existsAfterDelete.Value);
    }

    [Fact]
    public async Task List_returns_sources_under_prefix_with_full_logical_keys()
    {
        var storage = new InMemoryUserStorage();
        await storage.Save("adventures/one", ByteSource.FromString("1"));
        await storage.Save("adventures/two", ByteSource.FromString("2"));
        await storage.Save("settings/graphics", ByteSource.FromString("3"));

        var result = await storage.List("adventures");

        Assert.True(result.IsSuccess);
        Assert.Equal(["adventures/one", "adventures/two"], result.Value.Select(source => source.Key.Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("settings/../graphics")]
    [InlineData("settings\\graphics")]
    [InlineData("C:/settings")]
    public async Task Save_rejects_keys_with_filesystem_semantics(string key)
    {
        var storage = new InMemoryUserStorage();

        var result = await storage.Save((Path)key, ByteSource.FromBytes([]));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Save_rejects_separator_inside_manual_key_fragment()
    {
        var storage = new InMemoryUserStorage();

        var result = await storage.Save(new Path(["settings/graphics"]), ByteSource.FromBytes([]));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Save_copies_source_bytes()
    {
        var storage = new InMemoryUserStorage();
        var bytes = new byte[] { 1, 2, 3 };

        await storage.Save("files/data", ByteSource.FromBytes(bytes));
        bytes[0] = 9;
        var loaded = await storage.Load("files/data");
        var loadedBytes = await loaded.Value.Value.ReadAll();

        Assert.True(loadedBytes.IsSuccess);
        Assert.Equal([1, 2, 3], loadedBytes.Value);
    }
}
