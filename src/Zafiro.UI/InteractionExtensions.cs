using JetBrains.Annotations;

namespace Zafiro.UI;

[PublicAPI]
public static class InteractionExtensions
{
    public static IDisposable HandleErrorsWith<T>(this IObservable<Result<T>> result, INotificationService notificationService)
    {
        return HandleErrorsWith(result, notificationService, Maybe<string>.None);
    }

    public static IDisposable HandleErrorsWith(this IObservable<Result> result, INotificationService notificationService)
    {
        return HandleErrorsWith(result, notificationService, Maybe<string>.None);
    }

    public static IDisposable HandleErrorsWith<T>(this IObservable<Result<T>> result, INotificationService notificationService, Maybe<string> title)
    {
        return result.Failures().SelectMany(async error =>
        {
            await notificationService.Show(error, title);
            return Unit.Default;
        }).Subscribe();
    }

    public static IDisposable HandleErrorsWith(this IObservable<Result> result, INotificationService notificationService, Maybe<string> title)
    {
        return result.Failures().SelectMany(async error =>
        {
            await notificationService.Show(error, title);
            return Unit.Default;
        }).Subscribe();
    }

    public static Task Show(this INotificationService notificationService, string message)
    {
        return notificationService.Show(message, Maybe<string>.None);
    }
}