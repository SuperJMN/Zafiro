namespace Zafiro.UI.Jobs;

public static class JobExtensions
{
    public static IDisposable Execute(this IJob task)
    {
        return task.Execution.Start.Execute().Subscribe();
    }

    public static IDisposable OnCompleted(this IJob task, Action<IJob> onFinished)
    {
        return task
            .Execution.Start
            .Do(_ => onFinished(task))
            .Subscribe();
    }

    public static IDisposable OnStopped(this IJob task, Action<IJob> onFinished)
    {
        return task.Execution.Stop
            .Do(_ => onFinished(task))
            .Subscribe();
    }
}