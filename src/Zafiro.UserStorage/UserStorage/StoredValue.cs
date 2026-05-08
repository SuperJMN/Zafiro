namespace Zafiro.UserStorage;

public sealed record StoredValue<T>(Path Key, T Value);
