namespace Zafiro.UI;

public class MaybeViewModel<T>(Maybe<T> maybe)
{
    public T? Value => maybe.GetValueOrDefault();
    public bool HasValue => maybe.HasValue;
    public bool HasNoValue => maybe.HasNoValue;
}