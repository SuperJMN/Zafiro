using System;
using System.Reactive;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim.Builder;

/// <summary>
/// Intermediate builder for Next step configuration, supporting fluent guard specification.
/// </summary>
/// <typeparam name="TPrevious">The type of the previous step result.</typeparam>
/// <typeparam name="TPage">The current page type.</typeparam>
/// <typeparam name="TResult">The type produced by the next step.</typeparam>
public class NextStepBuilder<TPrevious, TPage, TResult>
{
    private readonly StepBuilder<TPrevious, TPage> stepBuilder;
    private readonly Func<TPage, TResult> selector;
    private readonly string? text;

    internal NextStepBuilder(StepBuilder<TPrevious, TPage> stepBuilder, Func<TPage, TResult> selector, string? text)
    {
        this.stepBuilder = stepBuilder;
        this.selector = selector;
        this.text = text;
    }

    /// <summary>
    /// Proceeds unconditionally (always enabled).
    /// </summary>
    public WizardBuilder<TResult> Always()
    {
        return stepBuilder.NextWith(page => EnhancedCommand.Create(() => Result.Success(selector(page)), Observable.Return(true), text));
    }

    /// <summary>
    /// Proceeds when the provided observable emits true.
    /// </summary>
    public WizardBuilder<TResult> When(Func<TPage, IObservable<bool>> canExecute)
    {
        return stepBuilder.NextWith(page => EnhancedCommand.Create(() => Result.Success(selector(page)), canExecute(page), text));
    }

    /// <summary>
    /// Proceeds when the page is valid (requires IValidatable).
    /// </summary>
    public WizardBuilder<TResult> WhenValid<TValidatablePage>() where TValidatablePage : TPage, IValidatable
    {
        return stepBuilder.NextWith(page => EnhancedCommand.Create(() => Result.Success(selector(page)), ((IValidatable)page).IsValid, text));
    }
}

/// <summary>
/// Intermediate builder for Next step with Result-returning selector.
/// </summary>
/// <typeparam name="TPrevious">The type of the previous step result.</typeparam>
/// <typeparam name="TPage">The current page type.</typeparam>
/// <typeparam name="TResult">The type produced by the next step.</typeparam>
public class NextResultStepBuilder<TPrevious, TPage, TResult>
{
    private readonly StepBuilder<TPrevious, TPage> stepBuilder;
    private readonly Func<TPage, Result<TResult>> selector;
    private readonly string? text;

    internal NextResultStepBuilder(StepBuilder<TPrevious, TPage> stepBuilder, Func<TPage, Result<TResult>> selector, string? text)
    {
        this.stepBuilder = stepBuilder;
        this.selector = selector;
        this.text = text;
    }

    /// <summary>
    /// Proceeds unconditionally (always enabled).
    /// </summary>
    public WizardBuilder<TResult> Always()
    {
        return stepBuilder.NextWith(page => EnhancedCommand.Create(() => selector(page), Observable.Return(true), text));
    }

    /// <summary>
    /// Proceeds when the provided observable emits true.
    /// </summary>
    public WizardBuilder<TResult> When(Func<TPage, IObservable<bool>> canExecute)
    {
        return stepBuilder.NextWith(page => EnhancedCommand.Create(() => selector(page), canExecute(page), text));
    }

    /// <summary>
    /// Proceeds when the page is valid (requires IValidatable).
    /// </summary>
    public WizardBuilder<TResult> WhenValid<TValidatablePage>() where TValidatablePage : TPage, IValidatable
    {
        return stepBuilder.NextWith(page => EnhancedCommand.Create(() => selector(page), ((IValidatable)page).IsValid, text));
    }
}

/// <summary>
/// Intermediate builder for Next step with (page, previous) selector.
/// </summary>
/// <typeparam name="TPrevious">The type of the previous step result.</typeparam>
/// <typeparam name="TPage">The current page type.</typeparam>
/// <typeparam name="TResult">The type produced by the next step.</typeparam>
public class NextStepWithPreviousBuilder<TPrevious, TPage, TResult>
{
    private readonly StepBuilder<TPrevious, TPage> stepBuilder;
    private readonly Func<TPage, TPrevious, TResult> selector;
    private readonly string? text;

    internal NextStepWithPreviousBuilder(StepBuilder<TPrevious, TPage> stepBuilder, Func<TPage, TPrevious, TResult> selector, string? text)
    {
        this.stepBuilder = stepBuilder;
        this.selector = selector;
        this.text = text;
    }

    /// <summary>
    /// Proceeds unconditionally (always enabled).
    /// </summary>
    public WizardBuilder<TResult> Always()
    {
        return stepBuilder.NextWith((page, prev) => EnhancedCommand.Create(() => Result.Success(selector(page, prev)), Observable.Return(true), text));
    }

    /// <summary>
    /// Proceeds when the provided observable emits true.
    /// </summary>
    public WizardBuilder<TResult> When(Func<TPage, TPrevious, IObservable<bool>> canExecute)
    {
        return stepBuilder.NextWith((page, prev) => EnhancedCommand.Create(() => Result.Success(selector(page, prev)), canExecute(page, prev), text));
    }

    /// <summary>
    /// Proceeds when the page is valid (requires IValidatable).
    /// </summary>
    public WizardBuilder<TResult> WhenValid<TValidatablePage>() where TValidatablePage : TPage, IValidatable
    {
        return stepBuilder.NextWith((page, prev) => EnhancedCommand.Create(() => Result.Success(selector(page, prev)), ((IValidatable)page).IsValid, text));
    }
}

/// <summary>
/// Intermediate builder for Next step with Result-returning selector that depends on previous result.
/// </summary>
/// <typeparam name="TPrevious">The type of the previous step result.</typeparam>
/// <typeparam name="TPage">The current page type.</typeparam>
/// <typeparam name="TResult">The type produced by the next step.</typeparam>
public class NextResultStepWithPreviousBuilder<TPrevious, TPage, TResult>
{
    private readonly StepBuilder<TPrevious, TPage> stepBuilder;
    private readonly Func<TPage, TPrevious, Result<TResult>> selector;
    private readonly string? text;

    internal NextResultStepWithPreviousBuilder(StepBuilder<TPrevious, TPage> stepBuilder, Func<TPage, TPrevious, Result<TResult>> selector, string? text)
    {
        this.stepBuilder = stepBuilder;
        this.selector = selector;
        this.text = text;
    }

    /// <summary>
    /// Proceeds unconditionally (always enabled).
    /// </summary>
    public WizardBuilder<TResult> Always()
    {
        return stepBuilder.NextWith((page, prev) => EnhancedCommand.Create(() => selector(page, prev), Observable.Return(true), text));
    }

    /// <summary>
    /// Proceeds when the provided observable emits true.
    /// </summary>
    public WizardBuilder<TResult> When(Func<TPage, TPrevious, IObservable<bool>> canExecute)
    {
        return stepBuilder.NextWith((page, prev) => EnhancedCommand.Create(() => selector(page, prev), canExecute(page, prev), text));
    }

    /// <summary>
    /// Proceeds when the page is valid (requires IValidatable).
    /// </summary>
    public WizardBuilder<TResult> WhenValid<TValidatablePage>() where TValidatablePage : TPage, IValidatable
    {
        return stepBuilder.NextWith((page, prev) => EnhancedCommand.Create(() => selector(page, prev), ((IValidatable)page).IsValid, text));
    }
}
