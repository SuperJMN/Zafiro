using CSharpFunctionalExtensions;
using Zafiro.ProgressReporting;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Jobs.Execution;

public class StoppableExecution : IExecution
{
    public StoppableExecution(IObservable<Unit> observable, IObservable<Progress> progress, Maybe<IObservable<bool>> canStart)
    {
        Progress = progress;
        var stoppable = StoppableCommand.Create(observable, canStart);
        Start = stoppable.StartReactive;
        Stop = stoppable.StopReactive;
    }

    public ReactiveCommandBase<Unit, Unit> Start { get; }
    public ReactiveCommandBase<Unit, Unit> Stop { get; }
    public IObservable<Progress> Progress { get; }
}