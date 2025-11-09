using Zafiro.ProgressReporting;

namespace Zafiro.UI.Jobs.Execution;

public class StartStopExecution(ReactiveCommandBase<Unit, Unit> start, ReactiveCommandBase<Unit, Unit> stop, IObservable<Progress> progress) : IExecution
{
    public ReactiveCommandBase<Unit, Unit> Start { get; } = start;
    public ReactiveCommandBase<Unit, Unit> Stop { get; } = stop;
    public IObservable<Progress> Progress { get; } = progress;
}