using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using Serilog;
using Zafiro.Reactive;

namespace Zafiro.CSharpFunctionalExtensions;

[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class FunctionalMixin
{
    public static async Task<IEnumerable<T>> Successes<T>(this Task<IEnumerable<Result<T>>> self)
    {
        var enumerable = await self.ConfigureAwait(false);
        return enumerable.Successes();
    }

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

    public static IEnumerable<T> Values<T>(this IEnumerable<Maybe<T>> self)
    {
        return self.Where(x => x.HasValue).Select(x => x.Value);
    }

    public static IEnumerable<string> Failures(this IEnumerable<Result> self)
    {
        return self.Where(a => a.IsFailure).Select(x => x.Error);
    }

    public static IEnumerable<string> Failures<T>(this IEnumerable<Result<T>> self)
    {
        return self.Where(a => a.IsFailure).Select(x => x.Error);
    }

    public static IEnumerable<T> Successes<T>(this IEnumerable<Result<T>> self)
    {
        return self.Where(a => a.IsSuccess)
            .Select(x => x.Value);
    }

    public static IEnumerable<string> NotNullOrEmpty(this IEnumerable<string> self)
    {
        return self.Where(s => !string.IsNullOrWhiteSpace(s));
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

    public static bool AnyEmpty<T>(this IEnumerable<Maybe<T>> self)
    {
        return self.Any(x => x.HasNoValue);
    }

    public static Maybe<TResult> Combine<T, TResult>(this IList<Maybe<T>> values, Func<IEnumerable<T>, TResult> combinerFunc)
    {
        if (values.AnyEmpty())
        {
            return Maybe<TResult>.None;
        }

        return Maybe.From(combinerFunc(values.Select(maybe => maybe.Value)));
    }

    public static Maybe<T> AsMaybe<T>(this Result<T> result)
    {
        if (result.IsFailure)
        {
            return Maybe<T>.None;
        }

        return Maybe.From(result.Value);
    }

    public static async Task<Maybe<T>> AsMaybe<T>(this Task<Result<T>> resultTask)
    {
        return (await resultTask.ConfigureAwait(false)).AsMaybe();
    }

    public static Result<TDestination> Cast<TSource, TDestination>(this Result<TSource> source, Func<TSource, TDestination> conversionFactory)
    {
        return source.Map(conversionFactory);
    }

    public static Task<Result<TDestination>> Cast<TSource, TDestination>(this Task<Result<TSource>> source, Func<TSource, TDestination> conversionFactory)
    {
        return source.Map(conversionFactory);
    }

    public static Task<Result<Maybe<TResult>>> Bind<TFirst, TResult>(
        this Task<Result<Maybe<TFirst>>> task,
        Func<TFirst, Task<Result<Maybe<TResult>>>> selector)
    {
        return task.Bind(maybe => maybe.Match(f => selector(f), () => Task.FromResult(Result.Success(Maybe<TResult>.None))));
    }

    // TODO: Test this
    public static Result<Maybe<TResult>> Bind<TFirst, TResult>(
        this Result<Maybe<TFirst>> task,
        Func<TFirst, Result<Maybe<TResult>>> selector)
    {
        return task.Bind(maybe => maybe.Match(f => selector(f), () => Result.Success(Maybe<TResult>.None)));
    }

    public static async Task<Result<IEnumerable<TResult>>> MapAndCombine<TInput, TResult>(
        this Result<IEnumerable<Task<Result<TInput>>>> enumerableOfTaskResults,
        Func<TInput, TResult> selector)
    {
        var result = await enumerableOfTaskResults.Map(async taskResults =>
        {
            var results = await Task.WhenAll(taskResults).ConfigureAwait(false);
            return results.Select(x => x.Map(selector)).Combine();
        }).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    ///     Binds and combines the results of the selector function applied to each item in the task of results.
    /// </summary>
    /// <typeparam name="T">The type of items in the input collection.</typeparam>
    /// <typeparam name="K">The type of items in the result collection.</typeparam>
    /// <param name="taskOfResults">A task that produces a Result of an IEnumerable of T.</param>
    /// <param name="selector">A function to apply to each item in the input collection.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result of an IEnumerable of K.</returns>
    public static Task<Result<IEnumerable<K>>> BindAndCombine<T, K>(
        this Task<Result<IEnumerable<T>>> taskOfResults,
        Func<T, Task<Result<K>>> selector)
    {
        return taskOfResults.Bind(async inputs =>
        {
            var tasksOfResult = inputs.Select(selector);
            var results = await Task.WhenAll(tasksOfResult).ConfigureAwait(false);
            return results.Combine();
        });
    }

    public static async Task<Result> Using(this Task<Result<Stream>> streamResult, Func<Stream, Task> useStream)
    {
        return await streamResult.Tap(async stream =>
        {
            await using (stream.ConfigureAwait(false))
            {
                await useStream(stream).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    public static async Task<Maybe<Task>> Tap<T>(this Task<Maybe<T>> maybeTask, Action<T> action)
    {
        var maybe = await maybeTask.ConfigureAwait(false);

        if (maybe.HasValue)
        {
            action(maybe.Value);
        }

        return maybeTask;
    }

    public static Maybe<T> Tap<T>(this Maybe<T> maybe, Action<T> action)
    {
        if (maybe.HasValue)
        {
            action(maybe.Value);
        }

        return maybe;
    }

    /// <summary>
    ///     Binds a collection of results to a function, and combines the results into a single task.
    /// </summary>
    /// <typeparam name="TInput">The type of the input values.</typeparam>
    /// <typeparam name="TResult">The type of the result values.</typeparam>
    /// <param name="taskResult">The task containing a collection of results.</param>
    /// <param name="selector">A function to apply to each result.</param>
    /// <returns>A task containing a collection of results after applying the selector function.</returns>
    public static Task<Result<IEnumerable<TResult>>> BindMany<TInput, TResult>(this Task<Result<IEnumerable<TInput>>> taskResult, Func<TInput, Result<TResult>> selector)
    {
        return taskResult.Bind(inputs => inputs.Select(selector).Combine());
    }

    public static async Task<Result<IEnumerable<TResult>>> Combine<TResult>(this IEnumerable<Task<Result<TResult>>> enumerableOfTaskResults, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        var results = await enumerableOfTaskResults
            .Select(task => Observable.FromAsync(() => task, scheduler ?? Scheduler.Default))
            .Merge(maxConcurrency)
            .ToList();

        return results.Combine();
    }

    public static Task<Result<IEnumerable<TResult>>> Combine<TResult>(this Task<Result<IEnumerable<Task<Result<TResult>>>>> task, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return task.Bind(tasks => Combine(tasks, scheduler, maxConcurrency));
    }

    public static Task<Result<IEnumerable<TResult>>> CombineSequentially<TResult>(this Task<Result<IEnumerable<Task<Result<TResult>>>>> task, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return task.Bind(tasks => CombineSequentially(tasks, scheduler));
    }

    public static Task<Result> CombineSequentially(this Task<Result<IEnumerable<Task<Result>>>> task, IScheduler? scheduler = default)
    {
        return task.Bind(tasks => CombineSequentially(tasks, scheduler));
    }

    public static async Task<Result> CombineSequentially(this IEnumerable<Task<Result>> enumerableOfTaskResults, IScheduler? scheduler = default)
    {
        var results = await enumerableOfTaskResults
            .Select(task => Observable.FromAsync(() => task, scheduler ?? Scheduler.Default))
            .Concat()
            .ToList();

        return results.Combine();
    }

    public static async Task<Result<IEnumerable<TResult>>> CombineSequentially<TResult>(this IEnumerable<Task<Result<TResult>>> enumerableOfTaskResults, IScheduler? scheduler = default)
    {
        var results = await enumerableOfTaskResults
            .Select(task => Observable.FromAsync(() => task, scheduler ?? Scheduler.Default))
            .Concat()
            .ToList();

        return results.Combine();
    }

    public static async Task<IEnumerable<Result<TResult>>> Concat<TResult>(this IEnumerable<Task<Result<TResult>>> enumerableOfTaskResults, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        var results = await enumerableOfTaskResults
            .Select(task => Observable.FromAsync(() => task, scheduler ?? Scheduler.Default))
            .Concat()
            .ToList();

        return results;
    }

    /// <summary>
    ///     Transforms the results of a task using a provided selector function.
    /// </summary>
    /// <typeparam name="TInput">The type of the input values.</typeparam>
    /// <typeparam name="TResult">The type of the result values.</typeparam>
    /// <param name="taskResult">The task containing a collection of results.</param>
    /// <param name="selector">A function to apply to each result.</param>
    /// <returns>A task containing a collection of results after applying the selector function.</returns>
    public static Task<Result<IEnumerable<TResult>>> MapEach<TInput, TResult>(this Task<Result<IEnumerable<TInput>>> taskResult, Func<TInput, TResult> selector)
    {
        return taskResult.Map(inputs => inputs.Select(selector));
    }

    public static Task<Result<IEnumerable<TResult>>> BindMany<TInput, TResult>(this Task<Result<IEnumerable<TInput>>> taskResult, Func<TInput, Task<Result<TResult>>> selector)
    {
        return taskResult.Bind(inputs => AsyncResultExtensionsLeftOperand.Combine(inputs.Select(selector)));
    }

    /// <summary>
    /// Transforms each input value using the provided transform function and combines all results with controlled concurrency.
    /// This is equivalent to MapEach followed by Combine. All transformations must succeed for the operation to succeed.
    /// </summary>
    /// <typeparam name="TInput">The type of values in the input collection.</typeparam>
    /// <typeparam name="TResult">The type of values produced by the transform function.</typeparam>
    /// <param name="result">A task containing a Result with a collection of input values.</param>
    /// <param name="transform">An async function that transforms each input value to a Result.</param>
    /// <param name="scheduler">The scheduler to use for task execution. Uses Scheduler.Default if not specified.</param>
    /// <param name="maxConcurrency">Maximum number of concurrent transformations. Defaults to 1 for sequential-like behavior.</param>
    /// <returns>A task containing a Result with all successful transformed values, or failure if any transformation failed.</returns>
    /// <example>
    /// <code>
    /// // Transform files concurrently with max 3 parallel operations
    /// var result = await filePathsResult.Traverse(
    ///     filePath => ProcessFileAsync(filePath), 
    ///     maxConcurrency: 3
    /// );
    /// </code>
    /// </example>
    public static Task<Result<IEnumerable<TResult>>> Traverse<TInput, TResult>(this Task<Result<IEnumerable<TInput>>> result, Func<TInput, Task<Result<TResult>>> transform, IScheduler? scheduler = null, int maxConcurrency = 1)
    {
        return result.MapEach(transform).Combine(scheduler, maxConcurrency);
    }

    /// <summary>
    /// Transforms each input value using the provided transform function and combines all results sequentially.
    /// This guarantees that transformations are executed one after another in order, which is useful when order matters
    /// or when transformations have side effects that must not overlap.
    /// </summary>
    /// <typeparam name="TInput">The type of values in the input collection.</typeparam>
    /// <typeparam name="TResult">The type of values produced by the transform function.</typeparam>
    /// <param name="result">A task containing a Result with a collection of input values.</param>
    /// <param name="transform">An async function that transforms each input value to a Result.</param>
    /// <returns>A task containing a Result with all successful transformed values in execution order, or failure if any transformation failed.</returns>
    /// <example>
    /// <code>
    /// // Process deployment steps in strict order
    /// var result = await deploymentStepsResult.TraverseSequentially(
    ///     step => ExecuteDeploymentStepAsync(step)
    /// );
    /// </code>
    /// </example>
    public static Task<Result<IEnumerable<TResult>>> TraverseSequentially<TInput, TResult>(
        this Task<Result<IEnumerable<TInput>>> result,
        Func<TInput, Task<Result<TResult>>> transform)
    {
        return result.MapEach(transform).CombineSequentially();
    }

    public static void Log(this Result result, ILogger? logger = default, string successString = "Success")
    {
        logger ??= Serilog.Log.Logger;

        result
            .Tap(() => logger.Information(successString))
            .TapError(logger.Error);
    }

    public static async Task Log(this Task<Result> result, ILogger? logger = default, string successString = "Success")
    {
        (await result.ConfigureAwait(false)).Log(logger ?? Serilog.Log.Logger, successString);
    }

    /// <summary>
    /// Returns the result of the task or, if the specified time elapses without completion,
    /// a failed Result indicating timeout.
    /// </summary>
    /// <typeparam name="T">Type of value wrapped in Result.</typeparam>
    /// <param name="task">Task that produces a Result&lt;T&gt;.</param>
    /// <param name="timeout">
    /// Maximum wait time. If null, no limit is applied.
    /// </param>
    /// <param name="message">
    /// Custom error message used when timeout occurs. Defaults to "Operation timed out".
    /// </param>
    public static async Task<Result<T>> WithTimeout<T>(
        this Task<Result<T>> task,
        TimeSpan timeout, string? message = "Operation timed out")
    {
        // Start parallel delay
        var delay = Task.Delay(timeout);

        // Wait for the first one to finish
        var finished = await Task.WhenAny(task, delay).ConfigureAwait(false);

        if (finished == task)
            return await task.ConfigureAwait(false);

        // Timeout was reached
        return Result.Failure<T>(message);
    }

    public static Task<Result<Maybe<T>>> TryFirstResult<T>(this IEnumerable<T> source, Func<T, Task<Result<bool>>> predicate)
    {
        // Assuming the application root contains a single ELF executable
        return source.Select(t => predicate(t).Map(b => (Matches: b, Item: t)))
            .CombineInOrder()
            .Map(bools => bools.TryFirst(tuple => tuple.Matches)
                .Select(tuple => tuple.Item));
    }

    public static Task<Result<T>> ToResult<T>(this Task<Result<Maybe<T>>> resultOfmaybe, string errorMessage)
    {
        return resultOfmaybe.Bind(x => x.ToResult(errorMessage));
    }
    
    public static Result<TResult> Select<T, TResult>(this Result<T> result, Func<T, TResult> selector)
    {
        return result.Map(selector);
    }
    
    public static Task<Result<TResult>> Select<T, TResult>(this Task<Result<T>> taskResult, Func<T, TResult> selector)
    {
        return taskResult.Map(selector);
    }
}