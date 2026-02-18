using System.ComponentModel;
using System.Windows.Input;

namespace Zafiro.UI.Commands;

public class EnhancedCommandWrapper<T> : IEnhancedCommand<T>
{
    private readonly IEnhancedCommand<T> inner;

    public EnhancedCommandWrapper(IEnhancedCommand<T> inner)
    {
        this.inner = inner;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return inner.Subscribe(observer);
    }

    public void Dispose()
    {
        inner.Dispose();
    }

    public IObservable<Exception> ThrownExceptions => inner.ThrownExceptions;

    public IObservable<bool> IsExecuting => inner.IsExecuting;

    public IObservable<bool> CanExecute => ((IReactiveCommand)inner).CanExecute;

    public IObservable<T> Execute(Unit parameter)
    {
        return inner.Execute(parameter);
    }

    public IObservable<T> Execute()
    {
        return inner.Execute();
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => inner.PropertyChanged += value;
        remove => inner.PropertyChanged -= value;
    }

    public event PropertyChangingEventHandler? PropertyChanging
    {
        add => inner.PropertyChanging += value;
        remove => inner.PropertyChanging -= value;
    }

    public void RaisePropertyChanging(PropertyChangingEventArgs args)
    {
        inner.RaisePropertyChanging(args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        inner.RaisePropertyChanged(args);
    }

    bool ICommand.CanExecute(object? parameter)
    {
        return inner.CanExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        inner.Execute(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => inner.CanExecuteChanged += value;
        remove => inner.CanExecuteChanged -= value;
    }

    public string? Name => inner.Name;

    public string? Text => inner.Text;
}

public static class EnhancedCommand
{
    // --- Result<T> variants (existing) ---
    public static IEnhancedCommand<Result> CreateWithResult(Func<Result> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.Create(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<Result> CreateWithResult(Func<Task<Result>> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.CreateFromTask(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<Result<T>> Create<T>(Func<Result<T>> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.Create(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<Result<T>> Create<T>(Func<Task<Result<T>>> task, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.CreateFromTask(task, canExecute).Enhance(text, name);
    }

    // --- Simple actions (no return value) ---
    public static IEnhancedCommand<Unit> Create(Action execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.Create(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<Unit> Create(Func<Task> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.CreateFromTask(execute, canExecute).Enhance(text, name);
    }

    // --- Functions with return value ---
    public static IEnhancedCommand<T> CreateWithResult<T>(Func<T> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.Create(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<T> CreateWithResult<T>(Func<Task<T>> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.CreateFromTask(execute, canExecute).Enhance(text, name);
    }

    // --- With input parameter ---
    public static IEnhancedCommand<TIn, TOut> Create<TIn, TOut>(Func<TIn, TOut> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.Create(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<TIn, TOut> Create<TIn, TOut>(Func<TIn, Task<TOut>> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.CreateFromTask(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<TIn, Unit> Create<TIn>(Action<TIn> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.Create(execute, canExecute).Enhance(text, name);
    }

    public static IEnhancedCommand<TIn, Unit> Create<TIn>(Func<TIn, Task> execute, IObservable<bool>? canExecute = null, string? text = null, string? name = null)
    {
        return ReactiveCommand.CreateFromTask(execute, canExecute).Enhance(text, name);
    }
}
