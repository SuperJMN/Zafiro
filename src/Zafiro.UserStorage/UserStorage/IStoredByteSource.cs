namespace Zafiro.UserStorage;

public interface IStoredByteSource
{
    Path Key { get; }
    IByteSource Content { get; }
}
