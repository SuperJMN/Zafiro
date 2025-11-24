using System;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim.Builder;

/// <summary>
/// Internal step definition used by the Slim wizard composition.
/// </summary>
public interface IStepDefinition
{
    /// <summary>Gets the step title (design-time/default).</summary>
    string Title { get; }

    /// <summary>
    /// Factory that creates the page instance for this step.
    /// </summary>
    /// <param name="previousResult">The previous step result or null.</param>
    /// <returns>The created page instance.</returns>
    object CreatePage(object? previousResult);

    /// <summary>
    /// Returns the Next command for the given page instance.
    /// </summary>
    /// <param name="page">The current page instance.</param>
    /// <returns>An enhanced command that computes the next result, or null when none.</returns>
    IEnhancedCommand<Result<object>>? GetNextCommand(object page);

    /// <summary>
    /// Returns the reactive title stream for the given page instance.
    /// </summary>
    /// <param name="page">The current page instance.</param>
    /// <returns>An observable title for this step and page.</returns>
    IObservable<string> GetTitle(object page);
}
