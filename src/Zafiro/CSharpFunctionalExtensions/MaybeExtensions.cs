using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;

namespace Zafiro.CSharpFunctionalExtensions;

[PublicAPI]
public static class MaybeExtensions
{
    public static Maybe<T> Tap<T>(this Maybe<T> maybe, Action<T> action)
    {
        if (maybe.HasValue)
        {
            action(maybe.Value);
        }

        return maybe;
    }

    public static Maybe<TResult> Combine<T, TResult>(this IList<Maybe<T>> values, Func<IEnumerable<T>, TResult> combinerFunc)
    {
        if (values.AnyEmpty())
        {
            return Maybe<TResult>.None;
        }

        return Maybe.From(combinerFunc(values.Select(maybe => maybe.Value)));
    }

    public static Result<TResult> MapMaybe<T, TResult>(this Maybe<Result<T>> maybeResult, Func<Maybe<T>, TResult> selector)
    {
        return maybeResult.Match(result =>
        {
            return result.Match(icon =>
            {
                var value = selector(icon);
                return Result.Success(value);
            }, Result.Failure<TResult>);
        }, () =>
        {
            var result = selector(Maybe.None);
            return Result.Success(result);
        });
    }


    public static Maybe<T> ToMaybe<T>(this T? value) where T : struct
    {
        if (value.HasValue)
        {
            return Maybe.From(value.Value);
        }

        return Maybe.None;
    }

    /// <summary>
    /// Transforms each element in the payload sequence of a Maybe&lt;IEnumerable&lt;TInput&gt;&gt; via <paramref name="selector"/>.
    /// </summary>
    /// <typeparam name="TInput">Type of elements in the input sequence.</typeparam>
    /// <typeparam name="TResult">Type of elements produced by <paramref name="selector"/>.
    /// </typeparam>
    /// <param name="input">Maybe containing a sequence to transform.</param>
    /// <param name="selector">Mapping function to apply to each element.
    /// </param>
    /// <returns>A Maybe&lt;IEnumerable&lt;TResult&gt;&gt; preserving the Maybe semantics.</returns>
    /// <remarks>
    /// Use when applying transformations inside optional sequences.
    /// </remarks>
    public static Maybe<IEnumerable<TResult>> MapEach<TInput, TResult>(this Maybe<IEnumerable<TInput>> input, Func<TInput, TResult> selector)
    {
        return input.Map(x => x.Select(selector));
    }
}
