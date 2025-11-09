using System;
using Zafiro.ProgressReporting;

namespace Zafiro.Works;

public interface IHaveProgress
{
    public IObservable<Progress> Progress { get; }
}