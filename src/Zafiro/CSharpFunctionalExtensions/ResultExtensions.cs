using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using Serilog;

namespace Zafiro.CSharpFunctionalExtensions;

[PublicAPI]
public static class ResultExtensions
{
    public static Maybe<T> AsMaybe<T>(this Result<T> result)
    {
        if (result.IsFailure)
        {
            return Maybe<T>.None;
        }

        return Maybe.From(result.Value);
    }

    // TODO: Test this
    public static Result<Maybe<TResult>> Bind<TFirst, TResult>(
        this Result<Maybe<TFirst>> task,
        Func<TFirst, Result<Maybe<TResult>>> selector)
    {
        return task.Bind(maybe => maybe.Match(f => selector(f), () => Result.Success(Maybe<TResult>.None)));
    }

    public static void Log(this Result result, ILogger? logger = default, string successString = "Success")
    {
        logger ??= Serilog.Log.Logger;

        result
            .Tap(() => logger.Information(successString))
            .TapError(logger.Error);
    }


    /// <summary>
    /// Performs a side-effect <paramref name="action"/> if the asynchronous <paramref name="condition"/> evaluates to true.
    /// </summary>
    /// <param name="result">Base Result to continue if condition is true.</param>
    /// <param name="condition">Asynchronous boolean condition.</param>
    /// <param name="action">Synchronous side-effect to invoke.
    /// </param>
    /// <returns>A Task of Result reflecting side-effect execution.
    /// </returns>
    /// <remarks>
    /// Useful when non-Result boolean checks govern execution in a Result pipeline.
    /// </remarks>
    public static async Task<Result> TapIfB(this Result result, Task<bool> condition, Func<Task> func)
    {
        return await condition.ConfigureAwait(false) ? await result.Tap(func).ConfigureAwait(false) : await Task.FromResult(result).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an asynchronous <paramref name="func"/> returning a payload of type <typeparamref name="T"/>
    /// if the asynchronous <paramref name="condition"/> is true, mapping into a Result&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">Type of the payload produced by the side-effect.</typeparam>
    /// <param name="result">Base Result to continue if condition is true.</param>
    /// <param name="condition">Asynchronous boolean condition.</param>
    /// <param name="func">Asynchronous function returning T on success.
    /// </param>
    /// <returns>A Task of Result&lt;T&gt; with payload or original failure.
    /// </returns>
    /// <remarks>
    /// Supports conditional asynchronous side-effect that yields a new Result with content.
    /// </remarks>
    public static async Task<Result<T>> TapIf<T>(this Result<T> result, Task<bool> condition, Func<Task<T>> func)
    {
        return await condition.ConfigureAwait(false) ? await result.Tap(func).ConfigureAwait(false) : await Task.FromResult(result).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies an asynchronous side-effect <paramref name="func"/> if the input Result&lt;T&gt; is successful.
    /// </summary>
    /// <typeparam name="T">Type of the input Result payload.</typeparam>
    /// <param name="result">Result&lt;T&gt; to process.</param>
    /// <param name="func">Asynchronous function that consumes the payload.
    /// </param>
    /// <returns>A Result indicating success or carrying the original error.
    /// </returns>
    /// <remarks>
    /// Preserves the functional style of applying effects only on successful Results.
    /// </remarks>
    public static async Task<Result> Map<T>(this Result<T> result, Func<T, Task> func)
    {
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        await func(result.Value).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Applies an asynchronous side-effect <paramref name="func"/> when the input Result succeeds.
    /// </summary>
    /// <param name="result">Result to process.</param>
    /// <param name="func">Asynchronous action to execute on success.
    /// </param>
    /// <returns>A Result indicating success or carrying the original error.
    /// </returns>
    /// <remarks>
    /// Use for executing generic asynchronous effects without payload.
    /// </remarks>
    public static async Task<Result> Map(this Result result, Func<Task> func)
    {
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        await func().ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Transforms each element in the payload sequence of a successful Result&lt;IEnumerable&lt;TInput&gt;&gt; via <paramref name="selector"/>.
    /// </summary>
    /// <typeparam name="TInput">Type of elements in the input sequence.</typeparam>
    /// <typeparam name="TResult">Type of elements produced by <paramref name="selector"/>.</typeparam>
    /// <param name="input">Result containing a sequence to transform.</param>
    /// <param name="selector">Mapping function to apply to each element.
    /// </param>
    /// <returns>A Result&lt;IEnumerable&lt;TResult&gt;&gt; on success or original failure.</returns>
    /// <remarks>
    /// Convenient for mapping over collections wrapped in a Result.
    /// </remarks>
    public static Result<IEnumerable<TResult>> MapEach<TInput, TResult>(this Result<IEnumerable<TInput>> input, Func<TInput, TResult> selector)
    {
        return input.Map(x => x.Select(selector));
    }

    /// <summary>
    /// Prepends an optional synchronous side-effect before an asynchronous Result task.
    /// </summary>
    /// <param name="result">Result wrapping a Task&lt;Result&gt; to invoke after <paramref name="prepend"/>.</param>
    /// <param name="prepend">Optional action to execute before the inner task.
    /// </param>
    /// <returns>A Task of Result after executing <paramref name="prepend"/> and the inner task.</returns>
    /// <remarks>
    /// Enables injecting synchronous logic prior to asynchronous Result computation.
    /// </remarks>
    public static Task<Result> Prepend(this Result<Task<Result>> result, Action? prepend = null)
    {
        return result.Bind(async task =>
        {
            prepend?.Invoke();
            return await task.ConfigureAwait(false);
        });
    }

    public static Result<K> CombineAndMap<T, Q, K>(this Result<T> one, Result<Q> another, Func<T, Q, K> combineFunction)
    {
        return one.Bind(x => another.Map(y => combineFunction(x, y)));
    }
}
