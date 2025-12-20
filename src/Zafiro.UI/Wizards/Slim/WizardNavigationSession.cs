using System.Reactive.Disposables;
using System.Threading;
using Zafiro.UI.Commands;
using Zafiro.UI.Navigation;

namespace Zafiro.UI.Wizards.Slim;

/// <summary>
/// Orchestrates navigation of a Slim wizard using an <see cref="INavigator"/>, ensuring a single subscription to
/// <see cref="ISlimWizard{T}.Finished"/> even if the navigator re-executes factories when going back.
/// </summary>
public sealed class WizardNavigationSession<TResult> : IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly Func<ISlimWizard<TResult>, INavigator, Task<bool>> cancelHandler;
    private readonly Func<IEnhancedCommand, object> contentFactory;
    private readonly TaskCompletionSource<Maybe<TResult>> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int completed;
    private NavigationBookmark? startBookmark;

    public WizardNavigationSession(
        ISlimWizard<TResult> wizard,
        INavigator navigator,
        Func<IEnhancedCommand, object> contentFactory,
        Func<ISlimWizard<TResult>, INavigator, Task<bool>>? cancel = null)
    {
        Wizard = wizard ?? throw new ArgumentNullException(nameof(wizard));
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        this.contentFactory = contentFactory ?? throw new ArgumentNullException(nameof(contentFactory));
        cancelHandler = cancel ?? ((_, _) => Task.FromResult(true));

        Cancel = ReactiveCommand.CreateFromTask(CancelAsync).Enhance();

        var finishedSubscription = Wizard.Finished
            .Take(1)
            .SelectMany(result => Observable.FromAsync(() => CompleteAsync(Maybe.From(result))))
            .Subscribe();

        disposables.Add(finishedSubscription);
    }

    public ISlimWizard<TResult> Wizard { get; }
    public INavigator Navigator { get; }

    /// <summary>Command that cancels the session and navigates back if the cancel handler allows it.</summary>
    public IEnhancedCommand Cancel { get; }

    /// <summary>Task that completes with the wizard result (Some) or cancellation (None).</summary>
    public Task<Maybe<TResult>> Completion => tcs.Task;

    /// <summary>Starts navigation to the wizard content.</summary>
    public Task<Result<Unit>> StartAsync()
    {
        startBookmark ??= Navigator.CreateBookmark();
        return Navigator.Go(() => contentFactory(Cancel));
    }

    private async Task CancelAsync()
    {
        var shouldClose = await cancelHandler(Wizard, Navigator);
        if (!shouldClose)
        {
            return;
        }

        await CompleteAsync(Maybe<TResult>.None);
    }

    private async Task CompleteAsync(Maybe<TResult> result)
    {
        if (Interlocked.Exchange(ref completed, 1) != 0)
        {
            return;
        }

        var backResult = startBookmark is not null
            ? await Navigator.GoBackTo(startBookmark)
            : await Navigator.GoBack();
        if (backResult.IsFailure)
        {
            tcs.TrySetException(new InvalidOperationException($"Failed to navigate back from wizard: {backResult.Error}"));
            return;
        }

        tcs.TrySetResult(result);
    }

    public void Dispose()
    {
        disposables.Dispose();
    }
}
