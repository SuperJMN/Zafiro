using System.ComponentModel;
using System.Windows.Input;

namespace Zafiro.UI.Commands;

public class EnhancedCommandWrapper<T, Q>(IEnhancedCommand<T, Q> implementation) : IEnhancedCommand<T, Q>
{
    public IDisposable Subscribe(IObserver<Q> observer)
    {
        return implementation.Subscribe(observer);
    }

    public void Dispose()
    {
        implementation.Dispose();
    }

    public IObservable<Exception> ThrownExceptions => implementation.ThrownExceptions;

    public IObservable<bool> IsExecuting => implementation.IsExecuting;

    public IObservable<bool> CanExecute => ((IReactiveCommand)implementation).CanExecute;

    public IObservable<Q> Execute(T parameter)
    {
        return implementation.Execute(parameter);
    }

    public IObservable<Q> Execute()
    {
        return implementation.Execute();
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => implementation.PropertyChanged += value;
        remove => implementation.PropertyChanged -= value;
    }

    public event PropertyChangingEventHandler? PropertyChanging
    {
        add => implementation.PropertyChanging += value;
        remove => implementation.PropertyChanging -= value;
    }

    public void RaisePropertyChanging(PropertyChangingEventArgs args)
    {
        implementation.RaisePropertyChanging(args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        implementation.RaisePropertyChanged(args);
    }

    bool ICommand.CanExecute(object? parameter)
    {
        return implementation.CanExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        implementation.Execute(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => implementation.CanExecuteChanged += value;
        remove => implementation.CanExecuteChanged -= value;
    }

    public string? Name => implementation.Name;

    public string? Text => implementation.Text;
}