using System;
using System.Reactive.Concurrency;
using Zafiro.UI;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim.Builder;

/// <summary>
/// Entry points to start composing a Slim wizard.
/// </summary>
public static class WizardBuilder
{
    /// <summary>
    /// Obsolete: prefer the overloads that return a StepBuilder.
    /// </summary>
    [Obsolete("Use methods that return StepBuilder instead.")]
    public static WizardBuilder<TResult> StartWith<TPage, TResult>(
        Func<TPage> pageFactory,
        Func<TPage, IEnhancedCommand<Result<TResult>>>? nextCommand,
        string title)
    {
        Func<TPage, Unit, IEnhancedCommand<Result<TResult>>>? command = nextCommand == null ? null : (page, _) => nextCommand(page);

        var step = new StepDefinition<Unit, TPage, TResult>(
            (_) => pageFactory(),
            command,
            title,
            (page, _) => Observable.Return(title ?? string.Empty));

        return new WizardBuilder<TResult>(new[] { step });
    }

    /// <summary>
    /// Obsolete: prefer the overloads that return a StepBuilder.
    /// </summary>
    [Obsolete("Use methods that return StepBuilder instead.")]
    public static WizardBuilder<TResult> StartWith<TPage, TResult>(
        Func<TPage> pageFactory,
        Func<TPage, Result<TResult>> nextAction,
        Func<TPage, IObservable<bool>>? canExecute,
        string title,
        string? text = null)
    {
        return StartWith(pageFactory, page => EnhancedCommand.Create(() => nextAction(page), canExecute?.Invoke(page), text), title);
    }

    /// <summary>
    /// Starts a wizard with the given page and static title.
    /// This is equivalent to providing an IObservable&lt;string&gt; via Observable.Return(title).
    /// </summary>
    public static StepBuilder<Unit, TPage> StartWith<TPage>(Func<TPage> pageFactory, string title)
    {
        return new StepBuilder<Unit, TPage>(
            Array.Empty<IStepDefinition>(),
            _ => pageFactory(),
            title,
            (page, _) => Observable.Return(title ?? string.Empty));
    }

    /// <summary>
    /// Starts a wizard using the page's own reactive title when it implements IHaveTitle.
    /// </summary>
    public static StepBuilder<Unit, TPage> StartWith<TPage>(Func<TPage> pageFactory)
        where TPage : IHaveTitle
    {
        return new StepBuilder<Unit, TPage>(
            Array.Empty<IStepDefinition>(),
            _ => pageFactory(),
            title: string.Empty,
            (page, _) => page.Title);
    }

    /// <summary>
    /// Starts a wizard with a page and a custom reactive title.
    /// </summary>
    public static StepBuilder<Unit, TPage> StartWith<TPage>(
        Func<TPage> pageFactory,
        Func<TPage, IObservable<string>> title)
    {
        return new StepBuilder<Unit, TPage>(
            Array.Empty<IStepDefinition>(),
            _ => pageFactory(),
            title: string.Empty,
            (page, _) => title(page));
    }
}

/// <summary>
/// Fluent composer that aggregates steps until the wizard is built.
/// </summary>
public class WizardBuilder<TResult>(IEnumerable<IStepDefinition> steps)
{
    private readonly List<IStepDefinition> steps = steps.ToList();

    /// <summary>
    /// Adds the next step, whose page depends on the previous result and has a static title.
    /// This is equivalent to providing an IObservable&lt;string&gt; via Observable.Return(title).
    /// </summary>
    public StepBuilder<TResult, TNextPage> Then<TNextPage>(Func<TResult, TNextPage> pageFactory, string title)
    {
        return new StepBuilder<TResult, TNextPage>(
            this.steps,
            prev => pageFactory(prev),
            title,
            (page, previous) => Observable.Return(title ?? string.Empty));
    }

    /// <summary>
    /// Adds the next step, whose page depends on the previous result and whose title comes from the page itself
    /// when it implements IHaveTitle.
    /// </summary>
    public StepBuilder<TResult, TNextPage> Then<TNextPage>(Func<TResult, TNextPage> pageFactory)
        where TNextPage : IHaveTitle
    {
        return new StepBuilder<TResult, TNextPage>(
            this.steps,
            prev => pageFactory(prev),
            title: string.Empty,
            (page, previous) => page.Title);
    }

    /// <summary>
    /// Adds the next step, whose page depends on the previous result and uses a custom reactive title.
    /// </summary>
    public StepBuilder<TResult, TNextPage> Then<TNextPage>(
        Func<TResult, TNextPage> pageFactory,
        Func<TNextPage, IObservable<string>> title)
    {
        return new StepBuilder<TResult, TNextPage>(
            this.steps,
            prev => pageFactory(prev),
            title: string.Empty,
            (page, previous) => title(page));
    }

    /// <summary>
    /// Obsolete: prefer the overloads that return a StepBuilder.
    /// </summary>
    [Obsolete("Use methods that return StepBuilder instead.")]
    public WizardBuilder<TNextResult> Then<TNextPage, TNextResult>(
        Func<TResult, TNextPage> pageFactory,
        Func<TNextPage, IEnhancedCommand<Result<TNextResult>>>? nextCommand,
        string title)
    {
        Func<TNextPage, TResult, IEnhancedCommand<Result<TNextResult>>>? command = nextCommand == null ? null : (page, _) => nextCommand(page);

        var step = new StepDefinition<TResult, TNextPage, TNextResult>(
            prev => pageFactory(prev),
            command,
            title);

        var newSteps = steps.Append(step);
        return new WizardBuilder<TNextResult>(newSteps);
    }

    /// <summary>
    /// Obsolete: prefer the overloads that return a StepBuilder.
    /// </summary>
    [Obsolete("Use methods that return StepBuilder instead.")]
    public WizardBuilder<TNextResult> Then<TNextPage, TNextResult>(
        Func<TResult, TNextPage> pageFactory,
        Func<TNextPage, Result<TNextResult>> nextAction,
        Func<TNextPage, IObservable<bool>>? canExecute,
        string title,
        string? text = null)
    {
        return Then(pageFactory, page => EnhancedCommand.Create(() => nextAction(page), canExecute?.Invoke(page), text), title);
    }

    /// <summary>
    /// Obsolete: prefer the overloads that return a StepBuilder.
    /// </summary>
    [Obsolete("Use methods that return StepBuilder instead.")]
    public WizardBuilder<TNextResult> Then<TNextPage, TNextResult>(
        Func<TResult, TNextPage> pageFactory,
        Func<TNextPage, TResult, IEnhancedCommand<Result<TNextResult>>>? nextCommand,
        string title)
    {
        Func<TNextPage, TResult, IEnhancedCommand<Result<TNextResult>>>? command = nextCommand == null ? null : (page, prev) => nextCommand(page, prev);

        var step = new StepDefinition<TResult, TNextPage, TNextResult>(
            prev => pageFactory(prev),
            command,
            title);

        var newSteps = steps.Append(step);
        return new WizardBuilder<TNextResult>(newSteps);
    }

    /// <summary>
    /// Obsolete: prefer the overloads that return a StepBuilder.
    /// </summary>
    [Obsolete("Use methods that return StepBuilder instead.")]
    public WizardBuilder<TNextResult> Then<TNextPage, TNextResult>(
        Func<TResult, TNextPage> pageFactory,
        Func<TNextPage, TResult, Result<TNextResult>> nextAction,
        Func<TNextPage, TResult, IObservable<bool>>? canExecute,
        string title,
        string? text = null)
    {
        return Then(pageFactory, (page, prev) => EnhancedCommand.Create(() => nextAction(page, prev), canExecute?.Invoke(page, prev), text), title);
    }

    /// <summary>
    /// Builds the wizard, marking the final step as Commit (allows navigating back from last page).
    /// </summary>
    public SlimWizard<TResult> WithCommitFinalStep()
    {
        return BuildWizard(StepKind.Commit);
    }

    /// <summary>
    /// Builds the wizard, marking the final step as Completion (prevents navigating back from last page).
    /// </summary>
    public SlimWizard<TResult> WithCompletionFinalStep()
    {
        return BuildWizard(StepKind.Completion);
    }

    /// <summary>
    /// Builds the wizard, marking the final step as Commit (allows navigating back from last page),
    /// using the provided scheduler to marshal state updates (typically UI thread).
    /// </summary>
    public SlimWizard<TResult> WithCommitFinalStep(IScheduler scheduler)
    {
        return BuildWizard(StepKind.Commit, scheduler);
    }

    /// <summary>
    /// Builds the wizard, marking the final step as Completion (prevents navigating back from last page),
    /// using the provided scheduler to marshal state updates (typically UI thread).
    /// </summary>
    public SlimWizard<TResult> WithCompletionFinalStep(IScheduler scheduler)
    {
        return BuildWizard(StepKind.Completion, scheduler);
    }

    private SlimWizard<TResult> BuildWizard(StepKind lastStepKind, IScheduler? scheduler = null)
    {
        List<IStepDefinition> normal = steps[..^1];
        IStepDefinition lastStep = steps[^1];

        var normalSteps = normal.Select(def => new WizardStep(
            StepKind.Normal,
            def.Title,
            def.CreatePage,
            def.GetNextCommand,
            def.GetTitle));
        var allSteps = normalSteps.Append(new WizardStep(
            lastStepKind,
            lastStep.Title,
            lastStep.CreatePage,
            lastStep.GetNextCommand,
            lastStep.GetTitle));

        return new SlimWizard<TResult>(allSteps.Cast<IWizardStep>().ToList(), scheduler);
    }
}
