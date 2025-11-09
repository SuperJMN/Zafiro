using CSharpFunctionalExtensions;

namespace Zafiro.UI.Commands;

public static class CommandExtensions
{
    public static IEnhancedCommand<T> Enhance<T>(this IEnhancedCommand<T> enhancedCommand, string? text = null, string? name = null, IObservable<bool>? canExecute = null)
    {
        // If no metadata or canExecute changes are requested, return the original command to avoid recreating it.
        if (text is null && name is null && canExecute is null)
        {
            return enhancedCommand;
        }

        // Otherwise, re-wrap the execution with the provided CanExecute and metadata.
        return new EnhancedReactiveCommandWrapper<T>(
            ReactiveCommand.CreateFromObservable(enhancedCommand.Execute, canExecute ?? ((IReactiveCommand)enhancedCommand).CanExecute),
            text ?? enhancedCommand.Text,
            name ?? enhancedCommand.Name);
    }

    public static IEnhancedCommand<T, Q> Enhance<T, Q>(this ReactiveCommandBase<T, Q> reactiveCommand, string? text = null, string? name = null)
    {
        return new EnhancedReactiveCommand<T, Q>(reactiveCommand, text, name);
    }

    public static IEnhancedCommand<T> Enhance<T>(this ReactiveCommandBase<Unit, T> reactiveCommand, string? text = null, string? name = null)
    {
        return new EnhancedReactiveCommandWrapper<T>(reactiveCommand, text, name);
    }

    public static IEnhancedCommand<Result> Enhance(this ReactiveCommandBase<Unit, Result<Unit>> reactiveCommand, string? text = null, string? name = null)
    {
        // Avoid creating a new ReactiveCommand: adapt the result type via CommandAdapter to preserve the original CanExecute/IsExecuting.
        var enhanced = new EnhancedReactiveCommandWrapper<Result<Unit>>(reactiveCommand, text, name);
        return new CommandAdapter<Result<Unit>, Result>(enhanced, r => (Result)r, name);
    }
}