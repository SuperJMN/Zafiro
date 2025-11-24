using Zafiro.Reactive;
using Zafiro.UI.Commands;
using Zafiro.UI.Wizards.Classic.Builder;

namespace Zafiro.UI.Wizards.Classic;

/// <summary>
/// Classic wizard implementation that navigates through steps and exposes a synchronous final result.
/// </summary>
public class Wizard<TResult> : ReactiveObject, IWizard<TResult>
{
    private readonly IList<IStep?> createdPages;
    private readonly IList<Func<IStep?, IStep>> pageFactories;
    private readonly Func<IStep, TResult> resultFactory;
    private IStep content;
    private int currentIndex = -1;

    /// <summary>
    /// Initializes a new Classic wizard.
    /// </summary>
    /// <param name="pages">Factories for each step page.</param>
    /// <param name="resultFactory">Factory that computes the final result from the last step.</param>
    public Wizard(List<Func<IStep?, IStep>> pages, Func<IStep, TResult> resultFactory)
    {
        this.resultFactory = resultFactory;
        pageFactories = pages;
        createdPages = pages.Select(_ => (IStep?)null).ToList();

        var hasNext = CreateHasNextObservable();
        IsValid = CreateIsValidObservable();
        IsBusy = CreateIsBusyObservable();
        IsLastPage = hasNext.Not();

        var nextCommand = CreateNextCommand(hasNext, IsValid);
        var backCommand = CreateBackCommand();

        SetupCommands(nextCommand, backCommand);
        InitializeWizard(nextCommand);
    }

    /// <summary>Gets the computed result once available.</summary>
    public TResult Result { get; private set; }

    /// <summary>Gets or sets the current page index.</summary>
    public int CurrentIndex
    {
        get => currentIndex;
        set => this.RaiseAndSetIfChanged(ref currentIndex, value);
    }

    /// <summary>Computes the final result from the last step.</summary>
    public TResult GetResult()
    {
        return resultFactory(Content);
    }

    /// <summary>Command to navigate backward.</summary>
    public IEnhancedCommand Back { get; private set; }
    /// <summary>Command to navigate forward.</summary>
    public IEnhancedCommand Next { get; private set; }

    /// <summary>Gets or sets the current step content.</summary>
    public IStep Content
    {
        get => content;
        set => this.RaiseAndSetIfChanged(ref content, value);
    }

    /// <summary>Signals whether the current step is the last page.</summary>
    public IObservable<bool> IsLastPage { get; }
    /// <summary>Signals whether the current step is valid.</summary>
    public IObservable<bool> IsValid { get; }
    /// <summary>Signals whether the current step is busy.</summary>
    public IObservable<bool> IsBusy { get; }
    /// <summary>Observable index of the current page.</summary>
    public IObservable<int> PageIndex => this.WhenAnyValue(x => x.CurrentIndex);
    /// <summary>Total number of pages.</summary>
    public int TotalPages => pageFactories.Count;

    private IObservable<bool> CreateHasNextObservable() =>
        this.WhenAnyValue(x => x.CurrentIndex)
            .Select(i => i < pageFactories.Count - 1);

    private IObservable<bool> CreateIsValidObservable() =>
        this.WhenAnyValue(x => x.Content)
            .WhereNotNull()
            .Select(x => x.IsValid)
            .Switch()
            .StartWith(false);

    private IObservable<bool> CreateIsBusyObservable() =>
        this.WhenAnyValue(x => x.Content)
            .WhereNotNull()
            .Select(x => x.IsBusy)
            .Switch()
            .StartWith(false);

    private ReactiveCommand<Unit, Unit> CreateNextCommand(IObservable<bool> hasNext, IObservable<bool> isValid)
    {
        var canGoNext = hasNext.CombineLatest(isValid, (h, v) => h && v);
        return ReactiveCommand.Create(NavigateNext, canGoNext);
    }

    private void NavigateNext()
    {
        CurrentIndex++;
        EnsurePageCreated();
        Content = createdPages[CurrentIndex]!;
    }

    private void EnsurePageCreated()
    {
        if (createdPages[CurrentIndex] != null) return;

        var previousPage = CurrentIndex > 0 ? createdPages[CurrentIndex - 1] : null;
        createdPages[CurrentIndex] = pageFactories[CurrentIndex](previousPage);
    }

    private ReactiveCommand<Unit, Unit> CreateBackCommand()
    {
        var isFirstPage = this.WhenAnyValue(x => x.CurrentIndex).Select(i => i == 0);
        var canGoBack = isFirstPage.CombineLatest(IsBusy, IsLastPage,
            (first, busy, last) => !first && !busy && !last);

        return ReactiveCommand.Create(NavigateBack, canGoBack);
    }

    private void NavigateBack()
    {
        createdPages[CurrentIndex] = null;
        CurrentIndex--;
        Content = createdPages[CurrentIndex]!;
    }

    private void SetupCommands(ReactiveCommand<Unit, Unit> next, ReactiveCommand<Unit, Unit> back)
    {
        Next = next.Enhance();
        Back = back.Enhance();
    }

    private void InitializeWizard(ReactiveCommand<Unit, Unit> nextCommand)
    {
        SetupAutoAdvance(nextCommand);
        nextCommand.Execute().Subscribe();
    }

    private IDisposable SetupAutoAdvance(ReactiveCommand<Unit, Unit> nextCommand) =>
        IsValid.Trues()
            .Where(_ => Content.AutoAdvance)
            .ToSignal()
            .InvokeCommand(nextCommand);
}
