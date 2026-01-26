using System;
using Zafiro.UI;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim.Builder;

/// <summary>
/// Default implementation of IStepDefinition for Slim wizards.
/// </summary>
/// <typeparam name="TPrevious">The type of the previous result.</typeparam>
/// <typeparam name="TPage">The page type.</typeparam>
/// <typeparam name="TResult">The type produced by the next step.</typeparam>
public class StepDefinition<TPrevious, TPage, TResult>(
    Func<TPrevious, TPage> pageFactory,
    Func<TPage, TPrevious, IEnhancedCommand<Result<TResult>>>? nextCommandFactory,
    string title,
    Func<TPage, TPrevious, IObservable<string>>? titleFactory = null)
    : IStepDefinition
{
    private readonly Func<TPage, TPrevious, IObservable<string>> resolvedTitleFactory =
        titleFactory ?? ((page, previous) => Observable.Return(title ?? string.Empty));

    private TPrevious previousResult = default!;

    /// <inheritdoc />
    public string Title { get; } = title;

    /// <inheritdoc />
    public object CreatePage(object? previousResult)
    {
        try
        {
            this.previousResult = previousResult is null ? default! : (TPrevious)previousResult;
        }
        catch (InvalidCastException)
        {
            throw new InvalidCastException($"Failed to cast object of type '{previousResult?.GetType().FullName}' to '{typeof(TPrevious).FullName}' in Step '{Title}'.");
        }
        return pageFactory(this.previousResult);
    }

    /// <inheritdoc />
    public IEnhancedCommand<Result<object>>? GetNextCommand(object page)
    {
        if (nextCommandFactory == null)
            return null;

        Console.Error.WriteLine($"[StepDefinition] GetNextCommand for '{Title}'. Page: {page.GetType().Name}. Prev: {previousResult?.GetType().Name}. Expected TResult: {typeof(TResult).FullName}");

        var typedPage = (TPage)page;
        var typedCommand = nextCommandFactory(typedPage, previousResult);
        return new CommandAdapter<Result<TResult>, Result<object>>(typedCommand, result =>
        {
            if (result.IsSuccess)
                Console.Error.WriteLine($"[CommandAdapter] Step '{Title}' converting result. TResult: {typeof(TResult).FullName}. Actual Value type: {result.Value?.GetType().FullName}");

            return result.Map(x => (object)x);
        });
    }

    /// <inheritdoc />
    public IObservable<string> GetTitle(object page)
    {
        var typedPage = (TPage)page;
        return resolvedTitleFactory(typedPage, previousResult);
    }
}
