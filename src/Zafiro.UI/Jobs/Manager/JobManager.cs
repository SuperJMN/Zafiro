using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DynamicData;

namespace Zafiro.UI.Jobs.Manager;

public class JobManager : IJobManager
{
    private readonly CompositeDisposable disposables = new();
    private readonly SourceCache<JobItem, string> tasks = new(x => x.Job.Id);

    public JobManager()
    {
        Tasks = tasks.Connect();
    }

    public IObservable<IChangeSet<JobItem, string>> Tasks { get; }

    public void Add(IJob job, JobOptions options)
    {
        if (options.RemoveOnCompleted)
        {
            job.Execution.Start
                .Delay(options.RemovalDelay, options.DelayScheduler)
                .Do(_ => tasks.Remove(job.Id))
                .Subscribe()
                .DisposeWith(disposables);
        }

        if (options.RemoveOnStopped && job.Execution.Stop is not null)
        {
            job.Execution.Stop
                .Delay(options.RemovalDelay, options.DelayScheduler)
                .Do(_ => tasks.Remove(job.Id))
                .Subscribe()
                .DisposeWith(disposables);
        }

        tasks.AddOrUpdate(new JobItem(job, options), new LambdaComparer<JobItem>((a, b) => Equals(a.Id, b.Id)));

        if (options.AutoStart)
        {
            job.Execute().DisposeWith(disposables);
        }
    }
}