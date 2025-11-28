using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.Mixins;
using Zafiro.UI.Commands;

namespace Zafiro.UI.Navigation;

public class Navigator : INavigator
{
    private readonly BehaviorSubject<bool> canGoBackSubject = new(false);
    private readonly BehaviorSubject<object?> contentSubject = new(null);
    private readonly Maybe<ILogger> logger;
    private readonly Dictionary<string, NavigationBookmark> namedBookmarks = new();
    private readonly Stack<Func<object>> navigationStack = new();
    private readonly IScheduler scheduler;
    private readonly IServiceProvider serviceProvider;
    private bool initialLoaded;
    private Func<Result<Unit>>? initialLoader;

    public Navigator(IServiceProvider serviceProvider, Maybe<ILogger> logger, IScheduler? scheduler, Func<Result<Unit>>? initialLoader = null)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.initialLoader = initialLoader;

        this.scheduler = scheduler ??
                         (SynchronizationContext.Current != null
                             ? new SynchronizationContextScheduler(SynchronizationContext.Current)
                             : Scheduler.Default);

        var reactiveCommand = ReactiveCommand.CreateFromTask(_ => GoBack(), canGoBackSubject.ObserveOn(this.scheduler));
        Back = reactiveCommand.Enhance();
    }

    public NavigationBookmark CreateBookmark()
    {
        return new NavigationBookmark(navigationStack.Count);
    }

    public void CreateBookmark(string name)
    {
        namedBookmarks[name] = new NavigationBookmark(navigationStack.Count);
    }

    public IObservable<object?> Content => Observable.Defer(() =>
    {
        EnsureInitial();
        return contentSubject.ObserveOn(scheduler);
    });

    public IEnhancedCommand<Result> Back { get; }

    public Task<Result<Unit>> Go(Func<object> factory)
    {
        return Task.FromResult(
            NavigateUsingFactory(factory)
                .TapError(error => logger.Error(error, "Navigation error - factory execution failed"))
        );
    }

    public Task<Result<Unit>> Go(Type type)
    {
        return Task.FromResult(
            NavigateUsingFactory(() => serviceProvider.GetRequiredService(type))
                .TapError(error => logger.Error(error, "Navigation error - service resolution failed for type {Type}", type.Name))
        );
    }

    public Task<Result<Unit>> GoBack()
    {
        return Task.FromResult(
            Result.Try(() => ExecuteGoBack())
                .TapError(error => logger.Error(error, "Navigation error - failed to go back"))
        );
    }

    public Task<Result<Unit>> GoBackTo(NavigationBookmark bookmark)
    {
        return Task.FromResult(
            Result.Try(() => ExecuteGoBackTo(bookmark))
                .TapError(error => logger.Error(error, "Navigation error - failed to go back to bookmark"))
        );
    }

    public Task<Result<Unit>> GoBackTo(string name)
    {
        return Task.FromResult(
            Result.Try(() =>
                {
                    if (!namedBookmarks.TryGetValue(name, out var bookmark))
                    {
                        throw new InvalidOperationException($"Bookmark '{name}' not found");
                    }

                    var result = ExecuteGoBackTo(bookmark);
                    namedBookmarks.Remove(name);
                    return result;
                })
                .TapError(error => logger.Error(error, "Navigation error - failed to go back to named bookmark {Name}", name))
        );
    }

    protected Result<Unit> NavigateUsingFactory(Func<object> factory)
    {
        return Result.Try(factory)
            .Tap(instance =>
            {
                navigationStack.Push(factory);
                contentSubject.OnNext(instance);
                canGoBackSubject.OnNext(navigationStack.Count > 1);
            })
            .Map(_ => Unit.Default);
    }

    private void EnsureInitial()
    {
        if (initialLoaded || initialLoader is null)
        {
            initialLoaded = true;
            return;
        }

        initialLoaded = true;
        var result = initialLoader();
        if (result.IsFailure)
        {
            logger.Error(result.Error, "Navigation error - failed to load initial content");
        }
    }

    protected void SetInitialLoader(Func<Result<Unit>> loader)
    {
        initialLoader = loader;
    }

    private Unit ExecuteGoBack()
    {
        if (navigationStack.Count <= 1)
        {
            throw new InvalidOperationException("No previous entries in the navigation stack");
        }

        navigationStack.Pop();
        RemoveBookmarksAfter(navigationStack.Count);

        if (navigationStack.Count > 0)
        {
            var previousFactory = navigationStack.Peek();
            contentSubject.OnNext(previousFactory());
        }
        else
        {
            contentSubject.OnNext(null);
        }

        canGoBackSubject.OnNext(navigationStack.Count > 1);

        return Unit.Default;
    }

    private Unit ExecuteGoBackTo(NavigationBookmark bookmark)
    {
        if (bookmark.Index < 0 || bookmark.Index > navigationStack.Count)
        {
            throw new InvalidOperationException("Bookmark out of range");
        }

        while (navigationStack.Count > bookmark.Index)
        {
            navigationStack.Pop();
        }

        RemoveBookmarksAfter(navigationStack.Count);

        var content = navigationStack.Count > 0 ? navigationStack.Peek()() : null;
        contentSubject.OnNext(content);
        canGoBackSubject.OnNext(navigationStack.Count > 1);
        return Unit.Default;
    }

    private void RemoveBookmarksAfter(int index)
    {
        var keysToRemove = namedBookmarks.Where(kv => kv.Value.Index > index).Select(kv => kv.Key).ToList();
        foreach (var key in keysToRemove)
        {
            namedBookmarks.Remove(key);
        }
    }
}