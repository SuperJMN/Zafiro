using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;

namespace Zafiro.CSharpFunctionalExtensions;

[PublicAPI]
public static class EnumerableExtensions
{
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

    public static bool AnyEmpty<T>(this IEnumerable<Maybe<T>> self)
    {
        return self.Any(x => x.HasNoValue);
    }

    public static Task<Result<Maybe<T>>> TryFirstResult<T>(this IEnumerable<T> source, Func<T, Task<Result<bool>>> predicate)
    {
        return source.Select(t => predicate(t).Map(b => (Matches: b, Item: t)))
            .CombineInOrder()
            .Map(bools => bools.TryFirst(tuple => tuple.Matches)
                .Select(tuple => tuple.Item));
    }
}
