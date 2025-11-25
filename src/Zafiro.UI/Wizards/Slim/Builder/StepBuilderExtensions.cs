using System.Reactive;
using Zafiro.UI.Commands;
using Zafiro.UI;

namespace Zafiro.UI.Wizards.Slim.Builder;

/// <summary>
/// Convenience extensions for StepBuilder to simplify common navigation patterns using a fluent API.
/// </summary>
public static class StepBuilderExtensions
{
    /// <summary>
    /// Starts defining the next step with a value selector from the page.
    /// Chain with .Always(), .When(), or .WhenValid() to specify the guard.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Next(page => page.SelectedValue).WhenValid()
    /// builder.Next(page => page.Result).Always()
    /// builder.Next(page => page.Id).When(page => page.IsReady)
    /// </code>
    /// </example>
    public static NextStepBuilder<TPrevious, TPage, TResult> Next<TPrevious, TPage, TResult>(
        this StepBuilder<TPrevious, TPage> builder,
        Func<TPage, TResult> selector,
        string? text = "Next")
    {
        return new NextStepBuilder<TPrevious, TPage, TResult>(builder, selector, text);
    }

    /// <summary>
    /// Starts defining the next step with a value selector from page and previous result.
    /// Chain with .Always(), .When(), or .WhenValid() to specify the guard.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Next((page, prev) => new State(page.Value, prev.Id)).WhenValid()
    /// builder.Next((page, prev) => prev).Always()
    /// </code>
    /// </example>
    public static NextStepWithPreviousBuilder<TPrevious, TPage, TResult> Next<TPrevious, TPage, TResult>(
        this StepBuilder<TPrevious, TPage> builder,
        Func<TPage, TPrevious, TResult> selector,
        string? text = "Next")
    {
        return new NextStepWithPreviousBuilder<TPrevious, TPage, TResult>(builder, selector, text);
    }

    /// <summary>
    /// Starts defining the next step with a Result-returning selector from the page.
    /// Use this when your selector can fail. Chain with .Always(), .When(), or .WhenValid() to specify the guard.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.NextResult(page => page.ValidateAndSave()).WhenValid()
    /// builder.NextResult(page => page.TryProcess()).Always()
    /// </code>
    /// </example>
    public static NextResultStepBuilder<TPrevious, TPage, TResult> NextResult<TPrevious, TPage, TResult>(
        this StepBuilder<TPrevious, TPage> builder,
        Func<TPage, Result<TResult>> selector,
        string? text = "Next")
    {
        return new NextResultStepBuilder<TPrevious, TPage, TResult>(builder, selector, text);
    }

    /// <summary>
    /// Starts defining the next step with a Result-returning selector from page and previous result.
    /// Use this when your selector can fail. Chain with .Always(), .When(), or .WhenValid() to specify the guard.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.NextResult((page, prev) => page.MergeWith(prev)).WhenValid()
    /// builder.NextResult((page, prev) => prev.Combine(page.Data)).Always()
    /// </code>
    /// </example>
    public static NextResultStepWithPreviousBuilder<TPrevious, TPage, TResult> NextResult<TPrevious, TPage, TResult>(
        this StepBuilder<TPrevious, TPage> builder,
        Func<TPage, TPrevious, Result<TResult>> selector,
        string? text = "Next")
    {
        return new NextResultStepWithPreviousBuilder<TPrevious, TPage, TResult>(builder, selector, text);
    }

    /// <summary>
    /// Defines the next step with a fully custom command that depends only on the current page.
    /// Use this when you need complete control over command creation, including custom can-execute logic,
    /// error handling, or side effects.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.NextCommand(page => 
    ///     EnhancedCommand.Create(
    ///         () => page.ComplexOperation(),
    ///         page.CustomCanExecute,
    ///         "Process"))
    /// </code>
    /// </example>
    public static WizardBuilder<TResult> NextCommand<TPrevious, TPage, TResult>(
        this StepBuilder<TPrevious, TPage> builder,
        Func<TPage, IEnhancedCommand<Result<TResult>>> commandFactory)
    {
        return builder.NextWith(commandFactory);
    }

    /// <summary>
    /// Defines the next step with a fully custom command that depends on both the current page and previous result.
    /// Use this when you need complete control over command creation, including custom can-execute logic,
    /// error handling, or side effects.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.NextCommand((page, prev) => 
    ///     EnhancedCommand.Create(
    ///         () => page.ProcessWith(prev),
    ///         Observable.CombineLatest(page.CanExecute, prev.IsReady, (c, r) => c && r),
    ///         "Process"))
    /// </code>
    /// </example>
    public static WizardBuilder<TResult> NextCommand<TPrevious, TPage, TResult>(
        this StepBuilder<TPrevious, TPage> builder,
        Func<TPage, TPrevious, IEnhancedCommand<Result<TResult>>> commandFactory)
    {
        return builder.NextWith(commandFactory);
    }

    /// <summary>
    /// Convenience method for steps that return Unit (typically final steps like Close, Finish, etc.).
    /// Chain with .Always(), .When(), or .WhenValid() to specify the guard.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.NextUnit("Close").Always()
    /// builder.NextUnit("Finish").When(page => page.CanFinish)
    /// </code>
    /// </example>
    public static NextStepBuilder<TPrevious, TPage, Unit> NextUnit<TPrevious, TPage>(
        this StepBuilder<TPrevious, TPage> builder,
        string? text = "Next")
    {
        return builder.Next(_ => Unit.Default, text);
    }

    // Type-safe WhenValid overloads based on TPage : IValidatable
    public static WizardBuilder<TResult> WhenValid<TPrevious, TPage, TResult>(
        this NextStepBuilder<TPrevious, TPage, TResult> builder)
        where TPage : IValidatable
    {
        return builder.When(page => page.IsValid);
    }

    public static WizardBuilder<TResult> WhenValid<TPrevious, TPage, TResult>(
        this NextResultStepBuilder<TPrevious, TPage, TResult> builder)
        where TPage : IValidatable
    {
        return builder.When(page => page.IsValid);
    }

    public static WizardBuilder<TResult> WhenValid<TPrevious, TPage, TResult>(
        this NextStepWithPreviousBuilder<TPrevious, TPage, TResult> builder)
        where TPage : IValidatable
    {
        return builder.When((page, _) => page.IsValid);
    }

    public static WizardBuilder<TResult> WhenValid<TPrevious, TPage, TResult>(
        this NextResultStepWithPreviousBuilder<TPrevious, TPage, TResult> builder)
        where TPage : IValidatable
    {
        return builder.When((page, _) => page.IsValid);
    }
}
