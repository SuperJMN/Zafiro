using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim;

/// <summary>
/// Slim wizard that drives navigation and exposes a final result when completed.
/// </summary>
/// <typeparam name="TResult">The type of the final result.</typeparam>
public sealed class SlimWizard<TResult> : ReactiveObject, ISlimWizard<TResult>, IBackCommandProvider
{
    private readonly IList<IWizardStep> steps;
    private readonly IScheduler scheduler;
    private readonly List<object?> results = new();

    private readonly object?[] pageInstances;
    private readonly IEnhancedCommand<Result<object>>?[] nextCommands;
    private readonly IObservable<string>?[] titleObservables;

    private readonly AsyncSubject<TResult> finishedSubject = new();

    private (int Index, IWizardStep Step) currentStep;
    private int currentStepIndex;
    private Page currentTypedPage = null!;
    private IPage currentPage = null!;
    private IEnhancedCommand<Result<object>> typedNext = null!;
    private IEnhancedCommand next = null!;
    private bool isFinished;

    /// <summary>
    /// Initializes a new SlimWizard with the provided steps.
    /// </summary>
    /// <param name="steps">The ordered list of step definitions.</param>
    public SlimWizard(IList<IWizardStep> steps)
        : this(steps, scheduler: null)
    {
    }

    /// <summary>
    /// Initializes a new SlimWizard with the provided steps and scheduler.
    /// </summary>
    /// <param name="steps">The ordered list of step definitions.</param>
    /// <param name="scheduler">Scheduler used to marshal wizard state updates (typically UI thread).</param>
    public SlimWizard(IList<IWizardStep> steps, IScheduler? scheduler)
    {
        EnsureValidSteps(steps);
        this.steps = steps;
        this.scheduler = scheduler ?? RxApp.MainThreadScheduler;
        TotalPages = steps.Count;

        pageInstances = new object?[TotalPages];
        nextCommands = new IEnhancedCommand<Result<object>>?[TotalPages];
        titleObservables = new IObservable<string>?[TotalPages];

        NavigateToIndex(0);

        var nextIsExecuting = this.WhenAnyValue(x => x.TypedNext)
            .Select(cmd => cmd.IsExecuting.StartWith(false))
            .Switch();

        var canGoBack = this.WhenAnyValue(x => x.CurrentStepIndex, x => x.IsFinished)
            .CombineLatest(nextIsExecuting, (state, executingNext) =>
            {
                var (index, finished) = state;

                if (finished || executingNext)
                {
                    return false;
                }

                if (index <= 0)
                {
                    return false;
                }

                if (index == TotalPages - 1 && this.steps[index].Kind == StepKind.Completion)
                {
                    return false;
                }

                return true;
            })
            .ObserveOn(this.scheduler);

        Back = ReactiveCommand.Create(NavigateBack, canGoBack).Enhance();

        Finished = finishedSubject.AsObservable();
    }

    /// <summary>Observable that emits the final result once and completes.</summary>
    public IObservable<TResult> Finished { get; }

    /// <summary>Command to navigate to the previous page, when allowed.</summary>
    public IEnhancedCommand Back { get; }

    /// <summary>Total number of pages in the wizard.</summary>
    public int TotalPages { get; }

    /// <summary>Gets the current step metadata.</summary>
    public (int Index, IWizardStep Step) CurrentStep
    {
        get => currentStep;
        private set => this.RaiseAndSetIfChanged(ref currentStep, value);
    }

    /// <summary>Gets the current step index.</summary>
    public int CurrentStepIndex
    {
        get => currentStepIndex;
        private set => this.RaiseAndSetIfChanged(ref currentStepIndex, value);
    }

    /// <summary>Gets the strongly-typed current page.</summary>
    public Page CurrentTypedPage
    {
        get => currentTypedPage;
        private set => this.RaiseAndSetIfChanged(ref currentTypedPage, value);
    }

    /// <summary>Gets the current page abstraction.</summary>
    public IPage CurrentPage
    {
        get => currentPage;
        private set => this.RaiseAndSetIfChanged(ref currentPage, value);
    }

    /// <summary>Gets the Next command that returns a Result&lt;object&gt;.</summary>
    public IEnhancedCommand<Result<object>> TypedNext
    {
        get => typedNext;
        private set => this.RaiseAndSetIfChanged(ref typedNext, value);
    }

    /// <summary>Gets the Next command adapted to a Unit-returning command.</summary>
    public IEnhancedCommand Next
    {
        get => next;
        private set => this.RaiseAndSetIfChanged(ref next, value);
    }

    private bool IsFinished
    {
        get => isFinished;
        set => this.RaiseAndSetIfChanged(ref isFinished, value);
    }

    private void NavigateBack()
    {
        if (IsFinished || CurrentStepIndex <= 0)
        {
            return;
        }

        ClearStepCache(CurrentStepIndex);

        if (results.Count > 0)
        {
            results.RemoveAt(results.Count - 1);
        }

        NavigateToIndex(CurrentStepIndex - 1);
    }

    private void NavigateToIndex(int index)
    {
        if (index < 0 || index >= TotalPages)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within wizard bounds.");
        }

        var step = steps[index];
        var pageInstance = GetOrCreatePage(index);
        var nextCommand = GetOrCreateNextCommand(index, pageInstance);
        var titleObservable = GetOrCreateTitleObservable(index, pageInstance);

        var page = new Page(index, pageInstance, nextCommand, step.Title, titleObservable, step.Kind);
        CurrentStep = (index, step);
        CurrentTypedPage = page;
        CurrentPage = page;
        TypedNext = nextCommand;
        Next = new CommandAdapter<Result<object>, Unit>(nextCommand, _ => Unit.Default);

        CurrentStepIndex = index;
    }

    private void HandleNextSuccess(object value)
    {
        if (IsFinished)
        {
            return;
        }

        if (CurrentStepIndex == TotalPages - 1)
        {
            IsFinished = true;
            finishedSubject.OnNext((TResult)value);
            finishedSubject.OnCompleted();
            return;
        }

        results.Add(value);
        NavigateToIndex(CurrentStepIndex + 1);
    }

    private object GetOrCreatePage(int index)
    {
        if (pageInstances[index] is { } existing)
        {
            return existing;
        }

        object? previousResult = null;
        if (index > 0)
        {
            if (results.Count < index)
            {
                throw new InvalidOperationException("Wizard internal state is inconsistent: missing previous result.");
            }

            previousResult = results[index - 1];
        }

        var step = steps[index];
        var page = step.CreatePage(previousResult) ??
                   throw new InvalidOperationException($"Wizard step at index {index} returned a null page.");

        pageInstances[index] = page;
        return page;
    }

    private IEnhancedCommand<Result<object>> GetOrCreateNextCommand(int index, object page)
    {
        if (nextCommands[index] is { } existing)
        {
            return existing;
        }

        var step = steps[index];
        var command = step.GetNextCommand(page) ??
                      throw new InvalidOperationException($"Wizard step at index {index} returned a null Next command.");

        var notFinished = this.WhenAnyValue(x => x.IsFinished)
            .Select(finished => !finished)
            .StartWith(!IsFinished);

        var canExecute = notFinished.CombineLatest(
            ((IReactiveCommand)command).CanExecute,
            (notFinishedValue, canExec) => notFinishedValue && canExec);

        var wrapped = ReactiveCommand.CreateFromObservable(
            execute: () => command.Execute()
                .ObserveOn(scheduler)
                .Do(result =>
                {
                    if (result.IsSuccess)
                    {
                        HandleNextSuccess(result.Value);
                    }
                }),
            canExecute: canExecute);

        var enhanced = wrapped.Enhance(text: command.Text, name: command.Name);
        nextCommands[index] = enhanced;
        return enhanced;
    }

    private IObservable<string> GetOrCreateTitleObservable(int index, object page)
    {
        if (titleObservables[index] is { } existing)
        {
            return existing;
        }

        var titleObservable = steps[index].GetTitle(page);
        titleObservables[index] = titleObservable;
        return titleObservable;
    }

    private void ClearStepCache(int index)
    {
        if (pageInstances[index] is IDisposable disposable)
        {
            disposable.Dispose();
        }

        pageInstances[index] = null;
        nextCommands[index] = null;
        titleObservables[index] = null;
    }

    private static void EnsureValidSteps(IList<IWizardStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Count == 0)
        {
            throw new ArgumentException("steps must contain at least one element.", nameof(steps));
        }
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] is null)
            {
                throw new ArgumentException($"steps[{i}] is null.", nameof(steps));
            }
        }
    }
}
