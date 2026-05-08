namespace Zafiro.UserStorage;

public sealed record StoredByteSource(Path Key, IByteSource Content) : IStoredByteSource;
