using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using Serilog;

namespace Zafiro.CSharpFunctionalExtensions;

[PublicAPI]
public static class TaskExtensions
{
    public static async Task<IEnumerable<T>> Successes<T>(this Task<IEnumerable<Result<T>>> self)
    {
        var enumerable = await self.ConfigureAwait(false);
        return enumerable.Successes();
    }

    public static async Task<Maybe<T>> AsMaybe<T>(this Task<Result<T>> resultTask)
    {
        return (await resultTask.ConfigureAwait(false)).AsMaybe();
    }



    public static Task<Result<Maybe<TResult>>> Bind<TFirst, TResult>(
        this Task<Result<Maybe<TFirst>>> task,
        Func<TFirst, Task<Result<Maybe<TResult>>>> selector)
    {
        return task.Bind(maybe => maybe.Match(f => selector(f), () => Task.FromResult(Result.Success(Maybe<TResult>.None))));
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

    private static IObservable<Result<TResult>> RunWithConcurrency<TResult>(IEnumerable<Func<Task<Result<TResult>>>> taskFactories, IScheduler scheduler, int maxConcurrency)
    {
        var sources = taskFactories
            .Select(factory => Observable.Defer(() => Observable.FromAsync(factory, scheduler)));

        return maxConcurrency <= 1
            ? sources.Concat()
            : sources.Merge(maxConcurrency);
    }

    private static IObservable<Result> RunWithConcurrency(IEnumerable<Func<Task<Result>>> taskFactories, IScheduler scheduler, int maxConcurrency)
    {
        var sources = taskFactories
            .Select(factory => Observable.Defer(() => Observable.FromAsync(factory, scheduler)));

        return maxConcurrency <= 1
            ? sources.Concat()
            : sources.Merge(maxConcurrency);
    }

    public static async Task<Result<IEnumerable<TResult>>> Combine<TResult>(this IEnumerable<Func<Task<Result<TResult>>>> taskFactories, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "maxConcurrency must be at least 1.");
        }

        var results = await RunWithConcurrency(taskFactories, scheduler ?? Scheduler.Default, maxConcurrency)
            .ToList();

        return results.Combine();
    }

    public static async Task<Result> Combine(this IEnumerable<Func<Task<Result>>> taskFactories, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        if (maxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "maxConcurrency must be at least 1.");
        }

        var results = await RunWithConcurrency(taskFactories, scheduler ?? Scheduler.Default, maxConcurrency)
            .ToList();

        return results.Combine();
    }

    public static Task<Result<IEnumerable<TResult>>> Combine<TResult>(this IEnumerable<Task<Result<TResult>>> enumerableOfTaskResults, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return enumerableOfTaskResults
            .Select(task => (Func<Task<Result<TResult>>>)(() => task))
            .Combine(scheduler, maxConcurrency);
    }

    public static Task<Result> Combine(this IEnumerable<Task<Result>> enumerableOfTaskResults, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return enumerableOfTaskResults
            .Select(task => (Func<Task<Result>>)(() => task))
            .Combine(scheduler, maxConcurrency);
    }

    public static Task<Result<IEnumerable<TResult>>> Combine<TResult>(this Task<Result<IEnumerable<Func<Task<Result<TResult>>>>>> task, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return task.Bind(tasks => tasks.Combine(scheduler, maxConcurrency));
    }

    public static Task<Result<IEnumerable<TResult>>> Combine<TResult>(this Task<Result<IEnumerable<Task<Result<TResult>>>>> task, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return task.Bind(tasks => tasks.Combine(scheduler, maxConcurrency));
    }

    public static Task<Result> Combine(this Task<Result<IEnumerable<Func<Task<Result>>>>> task, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return task.Bind(tasks => tasks.Combine(scheduler, maxConcurrency));
    }

    public static Task<Result> Combine(this Task<Result<IEnumerable<Task<Result>>>> task, IScheduler? scheduler = default, int maxConcurrency = 1)
    {
        return task.Bind(tasks => tasks.Combine(scheduler, maxConcurrency));
    }

    public static async Task<Result<IEnumerable<TResult>>> ExecuteSequentially<TResult>(this IEnumerable<Func<Task<Result<TResult>>>> enumerableOfTaskResults, IScheduler? scheduler = default)
    {
        var list = new List<TResult>();
        foreach (var func in enumerableOfTaskResults)
        {
            var result = await func().ConfigureAwait(false);
            if (result.IsFailure)
            {
                return Result.Failure<IEnumerable<TResult>>(result.Error);
            }
            list.Add(result.Value);
        }
        return Result.Success<IEnumerable<TResult>>(list);
    }

    public static async Task<Result> ExecuteSequentially(this IEnumerable<Func<Task<Result>>> enumerableOfTaskResults, IScheduler? scheduler = default)
    {
        foreach (var func in enumerableOfTaskResults)
        {
            var result = await func().ConfigureAwait(false);
            if (result.IsFailure)
            {
                return result;
            }
        }
        return Result.Success();
    }

    public static Task<Result<IEnumerable<TResult>>> ExecuteSequentially<TResult>(this Task<Result<IEnumerable<Func<Task<Result<TResult>>>>>> task, IScheduler? scheduler = default)
    {
        return task.Bind(tasks => tasks.ExecuteSequentially(scheduler));
    }

    public static Task<Result> ExecuteSequentially(this Task<Result<IEnumerable<Func<Task<Result>>>>> task, IScheduler? scheduler = default)
    {
        return task.Bind(tasks => tasks.ExecuteSequentially(scheduler));
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
    /// var result = await filePathsResult.MapConcurrently(
    ///     filePath => ProcessFileAsync(filePath), 
    ///     maxConcurrency: 3
    /// );
    /// </code>
    /// </example>
    public static Task<Result<IEnumerable<TResult>>> MapConcurrently<TInput, TResult>(this Task<Result<IEnumerable<TInput>>> result, Func<TInput, Task<Result<TResult>>> transform, IScheduler? scheduler = null, int maxConcurrency = 1)
    {
        return result.Bind(inputs =>
        {
            var operations = inputs.Select(input => (Func<Task<Result<TResult>>>)(() => transform(input)));
            return operations.Combine(scheduler, maxConcurrency);
        });
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
    /// var result = await deploymentStepsResult.MapSequentially(
    ///     step => ExecuteDeploymentStepAsync(step)
    /// );
    /// </code>
    /// </example>
    public static Task<Result<IEnumerable<TResult>>> MapSequentially<TInput, TResult>(
        this Task<Result<IEnumerable<TInput>>> result,
        Func<TInput, Task<Result<TResult>>> transform)
    {
        return result.Bind(inputs =>
        {
            var operations = inputs.Select(input => (Func<Task<Result<TResult>>>)(() => transform(input)));
            return operations.ExecuteSequentially();
        });
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

    public static Task<Result<T>> ToResult<T>(this Task<Result<Maybe<T>>> resultOfmaybe, string errorMessage)
    {
        return resultOfmaybe.Bind(x => x.ToResult(errorMessage));
    }



    /// <summary>
    /// Performs a side-effect <paramref name="func"/> if both the outer Result and <paramref name="conditionResult"/> succeed.
    /// </summary>
    /// <param name="resultTask">Asynchronous Result task to continue if condition passes.</param>
    /// <param name="conditionResult">Asynchronous condition returning Result&lt;bool&gt;.</param>
    /// <param name="func">Asynchronous side-effect to invoke on success.</param>
    /// <returns>A Task of Result after condition and side-effect.
    /// </returns>
    /// <remarks>
    /// Enables conditional execution of async actions based on multiple Result outcomes.
    /// </remarks>
    public static Task<Result> TapIf(this Task<Result> resultTask, Task<Result<bool>> conditionResult, Func<Task> func)
    {
        return conditionResult.Bind(condition => resultTask.TapIf(condition, func));
    }

    /// <summary>
    /// Performs a side-effect <paramref name="action"/> if both the outer Result and <paramref name="conditionResult"/> succeed.
    /// </summary>
    /// <param name="resultTask">Asynchronous Result task to continue if condition passes.</param>
    /// <param name="conditionResult">Asynchronous condition returning Result&lt;bool&gt;.</param>
    /// <param name="action">Synchronous side-effect to invoke on success.</param>
    /// <returns>A Task of Result after condition and side-effect.
    /// </returns>
    /// <remarks>
    /// Suitable when side-effect is synchronous and you want to conditionally augment a Result pipeline.
    /// </remarks>
    public static Task<Result> TapIf(this Task<Result> resultTask, Task<Result<bool>> conditionResult, Action action)
    {
        return conditionResult.Bind(condition => resultTask.TapIf(condition, action));
    }

    /// <summary>
    /// Negates the boolean payload of a successful Result&lt;bool&gt; asynchronously.
    /// </summary>
    /// <param name="result">Asynchronous Result&lt;bool&gt; to negate.</param>
    /// <returns>A Task of Result&lt;bool&gt; with inverted boolean.
    /// </returns>
    /// <remarks>
    /// Simplifies toggling boolean results in reactive or async contexts.
    /// </remarks>
    public static Task<Result<bool>> Not(this Task<Result<bool>> result)
    {
        return result.Map(b => !b);
    }

    /// <summary>
    /// Executes an asynchronous side-effect <paramref name="func"/> if the asynchronous <paramref name="condition"/> is true,
    /// continuing a Task&lt;Result&gt; chain.
    /// </summary>
    /// <param name="resultTask">Task of Result to continue if condition is true.</param>
    /// <param name="condition">Asynchronous boolean condition.</param>
    /// <param name="func">Asynchronous side-effect to invoke.
    /// </param>
    /// <returns>A Task of Result reflecting side-effect execution or original error.
    /// </returns>
    /// <remarks>
    /// Use when conditional logic is needed within async Result workflows.
    /// </remarks>
    public static async Task<Result> TapIf(this Task<Result> resultTask, Task<bool> condition, Func<Task> func)
    {
        return await condition.ConfigureAwait(false) ? await resultTask.Tap(func).ConfigureAwait(false) : await resultTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a synchronous side-effect <paramref name="action"/> if the asynchronous <paramref name="condition"/> is true,
    /// continuing a Task&lt;Result&gt; chain.
    /// </summary>
    /// <param name="resultTask">Task of Result to continue if condition is true.</param>
    /// <param name="condition">Asynchronous boolean condition.</param>
    /// <param name="action">Synchronous side-effect to invoke.
    /// </param>
    /// <returns>A Task of Result reflecting side-effect or original error.
    /// </returns>
    /// <remarks>
    /// Offers conditional synchronous side-effects within async Result pipelines.
    /// </remarks>
    public static async Task<Result> TapIf(this Task<Result> resultTask, Task<bool> condition, Action action)
    {
        return await condition.ConfigureAwait(false) ? await resultTask.Tap(action).ConfigureAwait(false) : await resultTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Applies an asynchronous side-effect <paramref name="func"/> on the payload of a Task&lt;Result&lt;T&gt;&gt;.
    /// </summary>
    /// <typeparam name="T">Type of the input Result payload.</typeparam>
    /// <param name="resultTask">Task of Result&lt;T&gt; to process.</param>
    /// <param name="func">Asynchronous function that consumes the payload.
    /// </param>
    /// <returns>A Task of Result indicating success or failure.
    /// </returns>
    /// <remarks>
    /// Facilitates performing asynchronous actions that do not alter the output but may have side-effects.
    /// </remarks>
    public static async Task<Result> Map<T>(this Task<Result<T>> resultTask, Func<T, Task> func)
    {
        return await (await resultTask.ConfigureAwait(false)).Map(func).ConfigureAwait(false);
    }

    public static Task<Result<K>> CombineAndMap<T, Q, K>(this Task<Result<T>> one, Task<Result<Q>> another, Func<T, Q, K> combineFunction)
    {
        return one.Bind(x => another.Map(y => combineFunction(x, y)));
    }

    public static Task<Result<K>> CombineAndBind<T, Q, K>(this Task<Result<T>> one, Task<Result<Q>> another, Func<T, Q, Result<K>> combineFunction)
    {
        return one.Bind(x => another.Bind(y => combineFunction(x, y)));
    }

    public static Task<Result> CombineAndBind<T, Q>(this Task<Result<T>> one, Task<Result<Q>> another, Func<T, Q, Result> combineFunction)
    {
        return one.Bind(x => another.Bind(y => combineFunction(x, y)));
    }

    public static Task<Result> CombineAndBind<T, Q>(this Task<Result<T>> one, Task<Result<Q>> another, Func<T, Q, Task<Result>> combineFunction)
    {
        return one.Bind(x => another.Bind(y => combineFunction(x, y)));
    }

    public static Task<Result<K>> CombineAndBind<T, Q, K>(this Task<Result<T>> one, Task<Result<Q>> another, Func<T, Q, Task<Result<K>>> combineFunction)
    {
        return one.Bind(x => another.Bind(y => combineFunction(x, y)));
    }
}
