using System;

namespace Zafiro.UI.Navigation;

public interface IHaveFooter
{
    IObservable<object> Footer { get; }
}
