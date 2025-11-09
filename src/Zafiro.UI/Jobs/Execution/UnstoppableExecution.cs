using CSharpFunctionalExtensions;
using Zafiro.ProgressReporting;
using Zafiro.Reactive;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Jobs.Execution;

public class UnstoppableExecution : IExecution
{
    public UnstoppableExecution(IObservable<Unit> observable, IObservable<Progress> progress)
    {
        Progress = progress;
        var stoppable = StoppableCommand.Create(observable.ToSignal, Maybe<IObservable<bool>>.None);
        Start = stoppable.StartReactive;
    }

    public ReactiveCommandBase<Unit, Unit> Start { get; }
    public ReactiveCommandBase<Unit, Unit>? Stop { get; } = null;
    public IObservable<Progress> Progress { get; }
}