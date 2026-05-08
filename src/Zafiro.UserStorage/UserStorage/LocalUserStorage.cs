using CSharpFunctionalExtensions;

namespace Zafiro.UserStorage;

public sealed class LocalUserStorage : IUserStorage
{
    private readonly string rootDirectory;

    private LocalUserStorage(string rootDirectory)
    {
        this.rootDirectory = rootDirectory;
    }

    public static LocalUserStorage ForApplication(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentException("Application name cannot be empty.", nameof(appName));
        }

        return new LocalUserStorage(GetApplicationRoot(appName.Trim()));
    }

    internal static LocalUserStorage ForDirectory(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory cannot be empty.", nameof(rootDirectory));
        }

        return new LocalUserStorage(rootDirectory);
    }

    public async Task<Result> Save(Path key, IByteSource content, CancellationToken cancellationToken = default)
    {
        return await UserStoragePath.ValidateFileKey(key)
            .Bind(() => content.ReadAll(cancellationToken))
            .Bind(bytes => Result.Try(async () =>
            {
                var path = GetFilePath(key);
                var directory = global::System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempName = "." + global::System.IO.Path.GetRandomFileName() + ".tmp";
                var temp = global::System.IO.Path.Combine(directory!, tempName);
                await File.WriteAllBytesAsync(temp, bytes, cancellationToken);
                Replace(temp, path);
            }));
    }

    public async Task<Result<Maybe<IByteSource>>> Load(Path key, CancellationToken cancellationToken = default)
    {
        return await UserStoragePath.ValidateFileKey(key)
            .Bind(() => Result.Try(async () =>
            {
                var path = GetFilePath(key);
                if (!File.Exists(path))
                {
                    return Maybe<IByteSource>.None;
                }

                var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
                return Maybe<IByteSource>.From(ByteSource.FromBytes(bytes));
            }));
    }

    public Task<Result> Delete(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UserStoragePath.ValidateFileKey(key)
            .Bind(() => Result.Try(() =>
            {
                var path = GetFilePath(key);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            })));
    }

    public Task<Result<bool>> Exists(Path key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UserStoragePath.ValidateFileKey(key)
            .Bind(() => Result.Try(() => File.Exists(GetFilePath(key)))));
    }

    public Task<Result<IReadOnlyList<IStoredByteSource>>> List(Path prefix, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(UserStoragePath.ValidatePrefix(prefix)
            .Bind(() => Result.Try<IReadOnlyList<IStoredByteSource>>(() =>
            {
                var root = GetPrefixPath(prefix);
                if (!Directory.Exists(root) && !File.Exists(root))
                {
                    return [];
                }

                var files = File.Exists(root)
                    ? [root]
                    : Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                        .Where(file => !global::System.IO.Path.GetFileName(file).EndsWith(".tmp", StringComparison.Ordinal))
                        .ToArray();

                return files
                    .Select(ToStoredByteSource)
                    .OrderBy(source => source.Key.Value, StringComparer.Ordinal)
                    .ToArray();
            })));
    }

    private IStoredByteSource ToStoredByteSource(string file)
    {
        var relative = global::System.IO.Path.GetRelativePath(rootDirectory, file)
            .Replace(global::System.IO.Path.DirectorySeparatorChar, Path.ChunkSeparator)
            .Replace(global::System.IO.Path.AltDirectorySeparatorChar, Path.ChunkSeparator);

        return new StoredByteSource(new Path(relative), ByteSource.FromBytes(File.ReadAllBytes(file)));
    }

    private string GetFilePath(Path key)
    {
        return key.RouteFragments.Aggregate(rootDirectory, global::System.IO.Path.Combine);
    }

    private string GetPrefixPath(Path prefix)
    {
        return prefix == Path.Empty ? rootDirectory : GetFilePath(prefix);
    }

    private static string GetApplicationRoot(string appName)
    {
        var specialFolder = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst()
            ? Environment.SpecialFolder.Personal
            : Environment.SpecialFolder.ApplicationData;

        var root = Environment.GetFolderPath(specialFolder);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = AppContext.BaseDirectory;
        }

        return global::System.IO.Path.Combine(root, appName);
    }

    private static void Replace(string source, string target)
    {
        try
        {
            if (File.Exists(target))
            {
                File.Replace(source, target, null, ignoreMetadataErrors: true);
                return;
            }

            File.Move(source, target);
        }
        catch
        {
            File.Move(source, target, overwrite: true);
        }
    }
}
