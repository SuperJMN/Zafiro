using System;
using CSharpFunctionalExtensions;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim.Builder;

/// <summary>
/// Concrete wizard step wrapper that adapts factories to IWizardStep.
/// </summary>
public class WizardStep : IWizardStep
{
    private readonly Func<object?, object> createPage;
    private readonly Func<object, IEnhancedCommand<Result<object>>?> getNextCommand;
    private readonly Func<object, IObservable<string>> getTitle;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardStep"/> class.
    /// </summary>
    /// <param name="kind">The step kind.</param>
    /// <param name="title">The static step title.</param>
    /// <param name="createPage">Factory to create the page given an optional previous result.</param>
    /// <param name="getNextCommand">Factory to get the Next command for a given page.</param>
    /// <param name="getTitle">Factory to get the reactive title for a given page.</param>
    public WizardStep(
        StepKind kind,
        string title,
        Func<object?, object> createPage,
        Func<object, IEnhancedCommand<Result<object>>?> getNextCommand,
        Func<object, IObservable<string>> getTitle)
    {
        this.createPage = createPage;
        this.getNextCommand = getNextCommand;
        this.getTitle = getTitle;
        Kind = kind;
        Title = title;
    }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public object CreatePage(object? previousResult)
    {
        return createPage(previousResult);
    }

    /// <inheritdoc />
    public IEnhancedCommand<Result<object>>? GetNextCommand(object page)
    {
        return getNextCommand(page);
    }

    /// <inheritdoc />
    public IObservable<string> GetTitle(object page)
    {
        return getTitle(page);
    }

    /// <inheritdoc />
    public StepKind Kind { get; }
}
