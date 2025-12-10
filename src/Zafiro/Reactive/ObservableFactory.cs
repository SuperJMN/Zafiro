using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace Zafiro.Reactive;

public static class ObservableFactory
{
    public static IObservable<TSource> UsingAsync<TSource, TResource>(
        Func<Task<TResource>> resourceFactoryAsync,
        Func<TResource, IObservable<TSource>> observableFactory)
        where TResource : IDisposable
    {
        return Observable.FromAsync(resourceFactoryAsync).SelectMany(
            resource => Observable.Using(() => resource, observableFactory));
    }
    
    public static IObservable<Result<TSource>> UsingAsync<TSource, TResource>(
        Func<Task<Result<TResource>>> resourceFactoryAsync,
        Func<TResource, IObservable<Result<TSource>>> observableFactory)
        where TResource : IDisposable
    {
        return Observable
            .FromAsync(resourceFactoryAsync)
            .SelectMany(resourceResult =>
                resourceResult.IsFailure
                    ? Observable.Return(Result.Failure<TSource>(resourceResult.Error))
                    : Observable.Using(
                        // This factory will be called once per subscription
                        () => resourceResult.Value,
                        observableFactory));
    }

}