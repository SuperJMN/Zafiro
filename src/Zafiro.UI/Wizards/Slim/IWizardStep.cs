using System;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim;

/// <summary>
/// Represents a single step definition within a Slim wizard.
/// </summary>
public interface IWizardStep
{
    /// <summary>Gets the static title of the step.</summary>
    string Title { get; }

    /// <summary>
    /// Creates the page (view model) for this step, optionally using the previous step's result.
    /// </summary>
    /// <param name="previousResult">The previous step result, or null if none.</param>
    /// <returns>The page instance.</returns>
    object CreatePage(object? previousResult);

    /// <summary>
    /// Gets the command to advance to the next step for the given page instance.
    /// </summary>
    /// <param name="page">The current page instance.</param>
    /// <returns>An enhanced command that yields the next result, or null if no Next action is available.</returns>
    IEnhancedCommand<Result<object>>? GetNextCommand(object page);

    /// <summary>
    /// Gets the reactive title stream for the given page instance.
    /// </summary>
    /// <param name="page">The current page instance.</param>
    /// <returns>An observable title for this step and page.</returns>
    IObservable<string> GetTitle(object page);

    /// <summary>Gets the kind of step (Normal, Commit, or Completion).</summary>
    StepKind Kind { get; }
}
