using System;

namespace Zafiro.UI.Navigation;

public interface IHaveHeader
{
    IObservable<object> Header { get; }
}
