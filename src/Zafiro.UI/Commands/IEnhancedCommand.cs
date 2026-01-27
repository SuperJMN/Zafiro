using System.Windows.Input;

namespace Zafiro.UI.Commands;

public interface IEnhancedCommand :
    IReactiveObject,
    ICommand,
    IReactiveCommand
{
    new IObservable<bool> CanExecute { get; }
    IObservable<bool> CanExecuteObservable => CanExecute;
    new IObservable<bool> IsExecuting { get; }
    IObservable<bool> IsExecutingObservable => IsExecuting;
    public string? Name { get; }
    public string? Text { get; }
}

public interface IEnhancedCommand<in T, out Q> : IReactiveCommand<T, Q>, IEnhancedCommand;

public interface IEnhancedCommand<out T> : IReactiveCommand<Unit, T>, IEnhancedCommand;