using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using Zafiro.Reactive;

namespace Zafiro.CSharpFunctionalExtensions;

[PublicAPI]
public static class ObservableExtensions
{
    public static IObservable<Unit> Successes(this IObservable<Result> self)
    {
        return self.Where(a => a.IsSuccess).ToSignal();
    }

    public static IObservable<T> Successes<T>(this IObservable<Result<T>> self)
    {
        return self.Where(a => a.IsSuccess).Select(x => x.Value);
    }

    public static IObservable<bool> IsSuccess<T>(this IObservable<Result<T>> self)
    {
        return self.Select(a => a.IsSuccess);
    }

    public static IObservable<bool> IsSuccess(this IObservable<Result> self)
    {
        return self.Select(a => a.IsSuccess);
    }

    public static IObservable<bool> IsFailure(this IObservable<Result> self)
    {
        return self.Select(a => a.IsFailure);
    }

    public static IObservable<string> Failures(this IObservable<Result> self)
    {
        return self.Where(a => a.IsFailure).Select(x => x.Error);
    }

    public static IObservable<string> Failures<T>(this IObservable<Result<T>> self)
    {
        return self.Where(a => a.IsFailure).Select(x => x.Error);
    }

    public static IObservable<T> Values<T>(this IObservable<Maybe<T>> self)
    {
        return self.Where(x => x.HasValue).Select(x => x.Value);
    }

    /// <summary>
    ///     Signals when the emitted item doesn't have a value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <returns></returns>
    public static IObservable<Unit> Empties<T>(this IObservable<Maybe<T>> self)
    {
        return self.Where(x => !x.HasValue).Select(_ => Unit.Default);
    }

    /// <summary>
    /// Projects each successful Result value of type <typeparamref name="T"/> in the observable sequence into a new Result of type <typeparamref name="K"/>
    /// by applying the specified synchronous <paramref name="function"/>.
    /// </summary>
    /// <typeparam name="T">Type of the input Result value.</typeparam>
    /// <typeparam name="K">Type of the output Result value.</typeparam>
    /// <param name="observable">Source sequence of Result&lt;T&gt; values.</param>
    /// <param name="function">Synchronous transformation to apply on each successful value.</param>
    /// <returns>An observable sequence of Result&lt;K&gt; values.</returns>
    /// <remarks>
    /// Use this method when you need to transform the payload of successful Results in a reactive pipeline.
    /// Failures propagate without invoking the <paramref name="function"/>.
    /// </remarks>
    public static IObservable<Result<K>> Map<T, K>(this IObservable<Result<T>> observable, Func<T, K> function)
    {
        return observable.Select(t => t.Map(function));
    }

    /// <summary>
    /// Projects each successful Result value of type <typeparamref name="T"/> into a new Result of type <typeparamref name="K"/>
    /// by applying the specified asynchronous <paramref name="function"/>.
    /// </summary>
    /// <typeparam name="T">Type of the input Result value.</typeparam>
    /// <typeparam name="K">Type of the output Result value.</typeparam>
    /// <param name="observable">Source sequence of Result&lt;T&gt; values.</param>
    /// <param name="function">Asynchronous transformation to apply on each successful value.</param>
    /// <returns>An observable sequence of Result&lt;K&gt; values.</returns>
    /// <remarks>
    /// This overload is suitable when the mapping requires asynchronous operations, such as I/O or network calls.
    /// </remarks>
    public static IObservable<Result<K>> Map<T, K>(this IObservable<Result<T>> observable, Func<T, Task<K>> function)
    {
        return observable.SelectMany(t => AsyncResultExtensionsRightOperand.Map(t, function));
    }

    /// <summary>
    /// Flattens and binds each successful Result value of type <typeparamref name="T"/> into a new asynchronous Result&lt;K&gt; via the specified <paramref name="function"/>.
    /// </summary>
    /// <typeparam name="T">Type of the input Result value.</typeparam>
    /// <typeparam name="K">Type of the output Result value.</typeparam>
    /// <param name="observable">Source sequence of Result&lt;T&gt; values.</param>
    /// <param name="function">Asynchronous binder returning Result&lt;K&gt;.</param>
    /// <returns>An observable sequence of Result&lt;K&gt; values after binding.</returns>
    /// <remarks>
    /// Use this method to chain asynchronous operations that return Result&lt;K&gt;, preserving failure propagation.
    /// </remarks>
    public static IObservable<Result<K>> Bind<T, K>(this IObservable<Result<T>> observable, Func<T, Task<Result<K>>> function)
    {
        return observable.SelectMany(t => t.Bind(function));
    }

    /// <summary>
    /// Flattens and binds each successful Result value of type <typeparamref name="T"/> into a new synchronous Result&lt;K&gt; via the specified <paramref name="function"/>.
    /// </summary>
    /// <typeparam name="T">Type of the input Result value.</typeparam>
    /// <typeparam name="K">Type of the output Result value.</typeparam>
    /// <param name="observable">Source sequence of Result&lt;T&gt; values.</param>
    /// <param name="function">Synchronous binder returning Result&lt;K&gt;.</param>
    /// <returns>An observable sequence of Result&lt;K&gt; values after binding.</returns>
    /// <remarks>
    /// This overload suits scenarios where binding logic is purely in-memory without asynchronous calls.
    /// </remarks>
    public static IObservable<Result<K>> Bind<T, K>(this IObservable<Result<T>> observable, Func<T, Result<K>> function)
    {
        return observable.Select(t => t.Bind(function));
    }

    /// <summary>
    /// Projects each successful Result value of type <typeparamref name="T"/> into a new observable sequence of Result&lt;K&gt; via the specified <paramref name="function"/>,
    /// flattening the resulting sequences into one.
    /// </summary>
    /// <typeparam name="T">Type of the input Result value.</typeparam>
    /// <typeparam name="K">Type of the inner sequence Result value.</typeparam>
    /// <param name="observable">Source sequence of Result&lt;T&gt; values.</param>
    /// <param name="function">Transformation producing an observable of Result&lt;K&gt; for each value.</param>
    /// <returns>An observable sequence of Result&lt;K&gt; values.</returns>
    /// <remarks>
    /// On failure of the inner observable or exception, the sequence emits a failure Result&lt;K&gt; with the exception message.
    /// Use this for branching reactive workflows based on Result values.
    /// </remarks>
    public static IObservable<Result<K>> SelectMany<T, K>(this IObservable<Result<T>> observable, Func<T, IObservable<Result<K>>> function)
    {
        return observable
            .SelectMany(result => result.IsSuccess
                ? function(result.Value)
                : Observable.Return(Result.Failure<K>(result.Error)))
            .Catch((Exception ex) => Observable.Return(Result.Failure<K>(ex.Message)));
    }

    /// <summary>
    /// Projects each successful Result value of type <typeparamref name="T"/> into a new observable sequence of Result via the specified <paramref name="function"/>,
    /// flattening the resulting sequences into one.
    /// </summary>
    /// <typeparam name="T">Type of the input Result value.</typeparam>
    /// <param name="observable">Source sequence of Result&lt;T&gt; values.</param>
    /// <param name="function">Transformation producing an observable of Result for each value.</param>
    /// <returns>An observable sequence of Result values.</returns>
    /// <remarks>
    /// Suited for reactive workflows where the continuation does not produce a value.
    /// </remarks>
    public static IObservable<Result> SelectMany<T>(this IObservable<Result<T>> observable, Func<T, IObservable<Result>> function)
    {
        return observable
            .SelectMany(result => result.IsSuccess
                ? function(result.Value)
                : Observable.Return(Result.Failure(result.Error)))
            .Catch((Exception ex) => Observable.Return(Result.Failure(ex.Message)));
    }

    /// <summary>
    /// Projects each successful Result value of type <typeparamref name="T"/> into an observable of Result&lt;K&gt;,
    /// then combines the original and inner Result values into a Result&lt;R&gt; via <paramref name="resultSelector"/>.
    /// </summary>
    /// <typeparam name="T">Type of the input Result value.</typeparam>
    /// <typeparam name="K">Type of the inner sequence Result value.</typeparam>
    /// <typeparam name="R">Type of the combined Result value.</typeparam>
    /// <param name="observable">Source sequence of Result&lt;T&gt; values.</param>
    /// <param name="collectionSelector">Transformation to an observable of Result&lt;K&gt;.</param>
    /// <param name="resultSelector">Combination function of T and K to produce an R.</param>
    /// <returns>An observable sequence of Result&lt;R&gt; values.</returns>
    /// <remarks>
    /// Facilitates joining two asynchronous pipelines of Results into one cohesive stream.
    /// </remarks>
    public static IObservable<Result<R>> SelectMany<T, K, R>(this IObservable<Result<T>> observable, Func<T, IObservable<Result<K>>> collectionSelector, Func<T, K, R> resultSelector)
    {
        return observable.SelectMany(result => result.IsSuccess
            ? collectionSelector(result.Value)
            : Observable.Empty<Result<K>>(), (result, result1) => result.CombineAndMap(result1, resultSelector));
    }

    /// <summary>
    /// Converts a source observable of <typeparamref name="T"/> into a source of Result&lt;T&gt; where each value is treated as success
    /// and exceptions are caught and mapped to failures with an optional <paramref name="errorMessage"/>.
    /// </summary>
    /// <typeparam name="T">Type of the source values.</typeparam>
    /// <param name="source">Sequence of values to wrap.</param>
    /// <param name="errorMessage">Optional error message for caught exceptions.</param>
    /// <returns>An observable of Result&lt;T&gt;.</returns>
    /// <remarks>
    /// Simplifies integration of legacy observables into a Result-based reactive pipeline.
    /// </remarks>
    public static IObservable<Result<T>> ToResult<T>(this IObservable<T> source, string? errorMessage = null)
    {
        return source.Select(Result.Success)
            .Catch<Result<T>, Exception>(ex =>
                Observable.Return(Result.Failure<T>(errorMessage ?? ex.Message)));
    }

    /// <summary>
    /// Applies a timeout to a sequence of Result&lt;T&gt; values, converting timeouts into failures with an optional message.
    /// </summary>
    /// <typeparam name="T">Type of the Result payload.</typeparam>
    /// <param name="source">Source of Result&lt;T&gt; values.</param>
    /// <param name="timeout">Maximum duration to wait for a value.</param>
    /// <param name="timeoutMessage">Optional message for timeout failures.
    /// </param>
    /// <returns>An observable of Result&lt;T&gt; with timeout handling.</returns>
    /// <remarks>
    /// Essential for ensuring responsiveness in reactive sequences that may stall.
    /// </remarks>
    public static IObservable<Result<T>> WithTimeout<T>(this IObservable<Result<T>> source,
        TimeSpan timeout,
        string? timeoutMessage = null)
    {
        return source.Timeout(timeout)
            .Catch<Result<T>, TimeoutException>(_ =>
                Observable.Return(Result.Failure<T>(timeoutMessage ?? "Operation timed out")));
    }
}
