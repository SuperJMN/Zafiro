using System;

namespace Zafiro.UI;

public interface IHaveTitle
{
    IObservable<string> Title { get; }
}
