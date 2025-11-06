using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using CSharpFunctionalExtensions;

namespace Zafiro.UI.Commands;

public static class StoppableCommand
{
    public static StoppableCommand<TIn, TOut> Create<TIn, TOut>(Func<TIn, IObservable<TOut>> logic, Maybe<IObservable<bool>> canStart)
    {
        return new StoppableCommand<TIn, TOut>(logic, canStart);
    }

    public static StoppableCommand<Unit, TOut> Create<TOut>(Func<IObservable<TOut>> logic, Maybe<IObservable<bool>> canStart)
    {
        return new StoppableCommand<Unit, TOut>(_ => logic(), canStart);
    }

    public static StoppableCommand<Unit, Unit> Create(IObservable<Unit> logic, Maybe<IObservable<bool>> canStart)
    {
        return new StoppableCommand<Unit, Unit>(_ => logic, canStart);
    }

    public static IStoppableCommand<Unit, TOut> CreateFromTask<TOut>(Func<CancellationToken, Task<TOut>> task, Maybe<IObservable<bool>> canStart)
    {
        return new StoppableCommand<Unit, TOut>(_ => Observable.FromAsync(task), canStart);
    }
}

public class StoppableCommand<TIn, TOut> : IStoppableCommand<TIn, TOut>
{
    public StoppableCommand(Func<TIn, IObservable<TOut>> logic, Maybe<IObservable<bool>> canStart)
    {
        var isExecuting = new Subject<bool>();
        StopReactive = ReactiveCommand.Create(() => { }, isExecuting);
        StartReactive = ReactiveCommand.CreateFromObservable<TIn, TOut>(e => logic(e).TakeUntil(StopReactive), canStart.GetValueOrDefault());
        StartReactive.IsExecuting.Subscribe(isExecuting);
    }

    public IObservable<bool> CanExecute => StartReactive.CanExecute;
    public ICommand Start => StartReactive;
    public ICommand Stop => StopReactive;
    public ReactiveCommandBase<Unit, Unit> StopReactive { get; }
    public ReactiveCommand<TIn, TOut> StartReactive { get; }
    public IObservable<bool> IsExecuting => StartReactive.IsExecuting;
}