using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using DynamicData;
using Zafiro.CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Mutable;

public class DirectoryWatcher
{
    private readonly SourceCache<IMutableNode, string> childrenCache = new(x => x.GetKey());
    private readonly CompositeDisposable disposable = new();

    public DirectoryWatcher(IMutableDirectory directory)
    {
        Directory = directory;
        directory.DirectoryCreated.Do(d => childrenCache.AddOrUpdate(d)).Subscribe();
        directory.FileCreated.Do(f => childrenCache.AddOrUpdate(f)).Subscribe();
        directory.DirectoryDeleted.Do(name => childrenCache.Remove(MutableMisc.GetDirKey(name))).Subscribe();
        directory.FileDeleted.Do(name => childrenCache.Remove(MutableMisc.GetFileKey(name))).Subscribe();

        Items = childrenCache.Connect();
    }

    public IObservable<IChangeSet<IMutableNode, string>> Items { get; }

    public IMutableDirectory Directory { get; }

    public IDisposable StartWatching()
    {
        return Children()
            .Select(x => x.Where(node => !node.IsHidden))
            .Do(nodes => childrenCache.EditDiff(nodes, (a, b) => a.GetKey() == b.GetKey()))
            .Subscribe()
            .DisposeWith(disposable);
    }

    private IObservable<IEnumerable<IMutableNode>> Children()
    {
        var initial = Observable.FromAsync(() => Directory.GetChildren());

        var timely = Observable
            .Timer(TimeSpan.FromSeconds(10))
            .Repeat()
            .Select(_ => Observable.FromAsync(() => Directory.GetChildren().Map(files => files.Where(x => !x.IsHidden))))
            .Switch();

        var results = initial.Concat(timely);

        return results.Successes();
    }

    public void Dispose()
    {
        childrenCache.Dispose();
    }
}