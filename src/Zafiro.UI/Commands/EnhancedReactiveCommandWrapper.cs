using System.Windows.Input;

namespace Zafiro.UI.Commands;

public class EnhancedReactiveCommandWrapper<T>(ReactiveCommandBase<Unit, T> reactiveCommandBase, string? text = null, string? name = null) : EnhancedReactiveCommand<Unit, T>(reactiveCommandBase, text, name), IEnhancedCommand<T>;

public class EnhancedReactiveCommand<TParam, TResult>(ReactiveCommandBase<TParam, TResult> reactiveCommandBase, string? text = null, string? name = null) : ReactiveObject, IEnhancedCommand<TParam, TResult>
{
    private readonly ICommand command = reactiveCommandBase;

    bool ICommand.CanExecute(object? parameter) => command.CanExecute(parameter);

    public void Execute(object? parameter) => command.Execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => command.CanExecuteChanged += value;
        remove => command.CanExecuteChanged -= value;
    }

    public new IObservable<Exception> ThrownExceptions => reactiveCommandBase.ThrownExceptions;

    public IObservable<bool> IsExecuting => reactiveCommandBase.IsExecuting;

    public IObservable<bool> CanExecute => ((IReactiveCommand)command).CanExecute;

    public IDisposable Subscribe(IObserver<TResult> observer) => reactiveCommandBase.Subscribe(observer);

    public IObservable<TResult> Execute(TParam parameter) => reactiveCommandBase.Execute(parameter);

    public IObservable<TResult> Execute() => reactiveCommandBase.Execute();

    public void Dispose()
    {
        reactiveCommandBase.Dispose();
    }

    public string? Name { get; } = name;
    public string? Text { get; } = text;
}