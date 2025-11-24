using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;

namespace Zafiro.UI.Reactive;

/// <summary>
/// Rx extensions to encapsulate the Throttle + Switch + busy-state pattern.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// - Optionally applies a throttle to the source sequence.
    /// - Emits <c>true</c> on <paramref name="busyObserver"/> before invoking the async call.
    /// - Calls <paramref name="asyncFunc"/>, converting the <see cref="Task{TResult}"/> to an <see cref="IObservable{TResult}"/>.
    /// - Emits <c>false</c> on <paramref name="busyObserver"/> upon completion or error.
    /// - Uses <see cref="Observable.Switch{TSource}(IObservable{IObservable{TSource}})"/> to keep ONLY the result of the most recent invocation.
    /// </summary>
    /// <param name="source">Input Observable (e.g., Feerate changes).</param>
    /// <param name="asyncFunc">Async function that produces the result.</param>
    /// <param name="busyObserver">Observer to signal busy status on/off.</param>
    /// <param name="throttle">Optional delay to group rapid events.</param>
    /// <param name="scheduler">Scheduler for Throttle (default is <c>DefaultScheduler.Instance</c>).</param>
    /// <typeparam name="TSource">Type of the source values.</typeparam>
    /// <typeparam name="TResult">Type of the Task result.</typeparam>
    /// <returns>
    /// An <see cref="IObservable{TResult}"/> with Throttle+Switch+Busy semantics.
    /// </returns>
    /// <example>
    /// <code>
    /// // In your ViewModel constructor:
    /// var drafts = this.WhenAnyValue(x => x.Feerate)
    ///     .WhereNotNull()
    ///     .SelectLatest(
    ///         fee => investmentAppService.CreateDraft(
    ///             walletId.Id.Value,
    ///             new ProjectId(project.Id),
    ///             new Amount(fee)
    ///         ),
    ///         isBusyObserver,
    ///         throttle: TimeSpan.FromSeconds(1)
    ///     )
    ///     .Select(tx => new InvestmentDraft(tx));
    ///
    /// drafts.Subscribe(draft => HandleDraft(draft));
    /// </code>
    /// </example>
    public static IObservable<TResult> SelectLatest<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, Task<TResult>> asyncFunc,
        IObserver<bool> busyObserver,
        TimeSpan? throttle = null,
        IScheduler? scheduler = null)
    {
        var sched = scheduler ?? DefaultScheduler.Instance;
        var stream = throttle.HasValue
            ? source.Throttle(throttle.Value, sched)
            : source;

        return stream
            .Select(item =>
                Observable.Defer(() =>
                    Observable.Return(item)
                        .Do(_ => busyObserver.OnNext(true))
                        .SelectMany(_ => asyncFunc(item).ToObservable())
                        .Finally(() => busyObserver.OnNext(false))
                )
            )
            .Switch();
    }

    /// <summary>
    /// Projects each element of an observable sequence into an observable sequence and merges the resulting sequences,
    /// emitting only the most recent inner observable sequence. Manages a busy-state observer during the processing.
    /// </summary>
    /// <typeparam name="TSource">Type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">Type of elements in the resulting sequence.</typeparam>
    /// <param name="source">The source observable sequence.</param>
    /// <param name="observableFunc">A function to invoke for each element, returning an observable sequence.</param>
    /// <param name="busyObserver">An observer indicating busy state (true when processing, false when completed).</param>
    /// <param name="throttle">Optional throttle duration to debounce rapid inputs.</param>
    /// <param name="scheduler">Scheduler used for throttling (default is <c>DefaultScheduler.Instance</c>).</param>
    /// <returns>An observable sequence emitting results from the most recent observable invocation.</returns>
    /// <example>
    /// <code>
    /// var results = inputObservable
    ///     .SelectLatest(
    ///         item => Observable.Start(() => HeavyComputation(item)),
    ///         busyObserver,
    ///         throttle: TimeSpan.FromMilliseconds(500)
    ///     );
    ///
    /// results.Subscribe(result => HandleResult(result));
    /// </code>
    /// </example>
    public static IObservable<TResult> SelectLatest<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, IObservable<TResult>> observableFunc,
        IObserver<bool> busyObserver,
        TimeSpan? throttle = null,
        IScheduler? scheduler = null)
    {
        var sched = scheduler ?? DefaultScheduler.Instance;
        var stream = throttle.HasValue
            ? source.Throttle(throttle.Value, sched)
            : source;

        return stream
            .Select(item =>
                Observable.Defer(() =>
                    Observable.Return(item)
                        .Do(_ => busyObserver.OnNext(true))
                        .SelectMany(_ => observableFunc(item))
                        .Finally(() => busyObserver.OnNext(false))
                )
            )
            .Switch();
    }
}