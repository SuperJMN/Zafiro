using Zafiro.UserStorage;

namespace Zafiro.UserStorage.Tests;

public class LocalUserStorageTests : IDisposable
{
    private readonly string root = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Save_load_and_list_roundtrips_without_public_paths()
    {
        var storage = LocalUserStorage.ForDirectory(root);

        Assert.True((await storage.Save("teams/default", ByteSource.FromString("team"))).IsSuccess);
        Assert.True((await storage.Save("teams/other", ByteSource.FromString("other"))).IsSuccess);

        var loaded = await storage.Load("teams/default");
        Assert.True(loaded.IsSuccess);
        var text = await loaded.Value.Value.ReadAllText();
        Assert.True(text.IsSuccess);
        Assert.Equal("team", text.Value);

        var list = await storage.List("teams");
        Assert.True(list.IsSuccess);
        Assert.Equal(["teams/default", "teams/other"], list.Value.Select(source => source.Key.Value));
    }

    [Fact]
    public async Task Save_overwrites_existing_content()
    {
        var storage = LocalUserStorage.ForDirectory(root);

        await storage.Save("settings/graphics", ByteSource.FromString("old"));
        await storage.Save("settings/graphics", ByteSource.FromString("new"));

        var loaded = await storage.Load("settings/graphics");
        var text = await loaded.Value.Value.ReadAllText();
        Assert.True(text.IsSuccess);
        Assert.Equal("new", text.Value);
    }

    [Fact]
    public async Task Delete_missing_key_succeeds()
    {
        var storage = LocalUserStorage.ForDirectory(root);

        Assert.True((await storage.Delete("missing/key")).IsSuccess);
    }

    public void Dispose()
    {
        if (global::System.IO.Directory.Exists(root))
        {
            global::System.IO.Directory.Delete(root, recursive: true);
        }
    }
}
