using CSharpFunctionalExtensions;

namespace Zafiro.UserStorage;

internal static class UserStoragePath
{
    private static readonly char[] InvalidChars = [Path.ChunkSeparator, '\\', ':', '*', '?', '"', '<', '>', '|'];

    public static Result ValidateFileKey(Path key)
    {
        return Validate(key, allowEmpty: false);
    }

    public static Result ValidatePrefix(Path prefix)
    {
        return Validate(prefix, allowEmpty: true);
    }

    public static bool IsUnderPrefix(Path key, Path prefix)
    {
        if (prefix == Path.Empty)
        {
            return true;
        }

        var keyValue = key.Value;
        var prefixValue = prefix.Value;
        return string.Equals(keyValue, prefixValue, StringComparison.Ordinal) ||
               keyValue.StartsWith(prefixValue + Path.ChunkSeparator, StringComparison.Ordinal);
    }

    private static Result Validate(Path path, bool allowEmpty)
    {
        var fragments = path.RouteFragments.ToArray();
        if (fragments.Length == 0)
        {
            return allowEmpty
                ? Result.Success()
                : Result.Failure("Storage key cannot be empty.");
        }

        return fragments.Select(ValidateFragment).Combine();
    }

    private static Result ValidateFragment(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
        {
            return Result.Failure("Storage key segments cannot be empty.");
        }

        if (fragment is "." or "..")
        {
            return Result.Failure("Storage key segments cannot be relative path markers.");
        }

        return fragment.IndexOfAny(InvalidChars) >= 0
            ? Result.Failure($"Storage key segment '{fragment}' contains invalid characters.")
            : Result.Success();
    }
}
