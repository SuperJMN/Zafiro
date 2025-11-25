using Zafiro.ProgressReporting;
using Zafiro.Reactive;

namespace Zafiro.UI.Jobs.Execution;

public abstract class ExecutionFactory
{
    public static IExecution From<T>(IObservable<T> onExecute, IObservable<Progress> progress, Maybe<IObservable<bool>> canStart)
    {
        return new StoppableExecution(onExecute.ToSignal(), progress, canStart);
    }

    public static IExecution From(Func<CancellationToken, Task> taskFactory, IObservable<Progress> progress, Maybe<IObservable<bool>> canStart)
    {
        return new StoppableExecution(Observable.FromAsync(taskFactory).ToSignal(), progress, canStart);
    }

    public static IExecution From<T>(Func<CancellationToken, Task<T>> taskFactory, IObservable<Progress> progress, Maybe<IObservable<bool>> canStart)
    {
        return new StoppableExecution(Observable.FromAsync(taskFactory).ToSignal(), progress, canStart);
    }

    public static IExecution From(Func<Task> taskFactory, IObservable<Progress> progress)
    {
        return new UnstoppableExecution(Observable.FromAsync(taskFactory).ToSignal(), progress);
    }

    public static IExecution From(ReactiveCommandBase<Unit, Unit> start, ReactiveCommandBase<Unit, Unit> stop, IObservable<Progress> progress)
    {
        return new StartStopExecution(start, stop, progress);
    }
}