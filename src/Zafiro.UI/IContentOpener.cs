namespace Zafiro.UI;

public interface IContentOpener
{
    Task<Result> Open(IObservable<byte> contents, string name);
}