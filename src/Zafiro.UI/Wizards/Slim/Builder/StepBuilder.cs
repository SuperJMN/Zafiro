using System;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using Zafiro.UI;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim.Builder;

/// <summary>
/// Builds a wizard step for a given page type, allowing you to define how to proceed to the next step.
/// </summary>
/// <typeparam name="TPrevious">The type of the previous step result.</typeparam>
/// <typeparam name="TPage">The type of the current page view model.</typeparam>
public class StepBuilder<TPrevious, TPage>(
    IEnumerable<IStepDefinition> previousSteps,
    Func<TPrevious, TPage> pageFactory,
    string title,
    Func<TPage, TPrevious, IObservable<string>>? titleFactory = null)
{
    private readonly IEnumerable<IStepDefinition> previousSteps = previousSteps;
    private readonly Func<TPrevious, TPage> pageFactory = pageFactory;
    private readonly string title = title;
    private readonly Func<TPage, TPrevious, IObservable<string>>? titleFactory = titleFactory;

    private Func<TPage, TPrevious, IObservable<string>> ResolveTitleFactory()
    {
        if (titleFactory != null)
        {
            return titleFactory;
        }

        // Default case: use the static title provided to the builder.
        return (page, previous) => Observable.Return(title ?? string.Empty);
    }

    /// <summary>
    /// Creates the next step using a command that depends only on the current page.
    /// </summary>
    /// <typeparam name="TResult">The type produced by the next step.</typeparam>
    /// <param name="nextCommand">Factory that receives the current page and returns the Next command.</param>
    /// <returns>A wizard builder that continues with the specified next step.</returns>
    public WizardBuilder<TResult> NextWith<TResult>(Func<TPage, IEnhancedCommand<Result<TResult>>> nextCommand)
    {
        var step = new StepDefinition<TPrevious, TPage, TResult>(
            pageFactory,
            (page, _) => nextCommand(page),
            title,
            ResolveTitleFactory());
        var steps = previousSteps.Append(step);
        return new WizardBuilder<TResult>(steps);
    }

    /// <summary>
    /// Creates the next step using a command that depends on both the current page and the previous result.
    /// </summary>
    /// <typeparam name="TResult">The type produced by the next step.</typeparam>
    /// <param name="nextCommand">Factory that receives the current page and the previous result, returning the Next command.</param>
    /// <returns>A wizard builder that continues with the specified next step.</returns>
    public WizardBuilder<TResult> NextWith<TResult>(Func<TPage, TPrevious, IEnhancedCommand<Result<TResult>>> nextCommand)
    {
        var step = new StepDefinition<TPrevious, TPage, TResult>(
            pageFactory,
            nextCommand,
            title,
            ResolveTitleFactory());
        var steps = previousSteps.Append(step);
        return new WizardBuilder<TResult>(steps);
    }

    [Obsolete("Use NextWith instead.")]
    public WizardBuilder<TResult> ProceedWith<TResult>(Func<TPage, IEnhancedCommand<Result<TResult>>> nextCommand)
        => NextWith(nextCommand);

    [Obsolete("Use NextWith instead.")]
    public WizardBuilder<TResult> ProceedWith<TResult>(Func<TPage, TPrevious, IEnhancedCommand<Result<TResult>>> nextCommand)
        => NextWith(nextCommand);
}
