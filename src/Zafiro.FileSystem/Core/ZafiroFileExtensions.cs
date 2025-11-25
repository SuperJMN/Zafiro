using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using Zafiro.Actions;
using Zafiro.CSharpFunctionalExtensions;
using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public static class ZafiroFileExtensions
{
    public static Task<Result> Copy(this IZafiroFile source, IZafiroFile destination, Maybe<IObserver<LongProgress>> progress, IScheduler? progressScheduler = default, TimeSpan? readTimeout = default,
        CancellationToken cancellationToken = default)
    {
        return
            source.GetData().CombineAndMap(source.Properties, (st, pr) => CreateCompatibleStream(st, pr))
                .Bind(async stream =>
                {
                    var subscription = progress.Map(p => stream.Positions.Select(x => new LongProgress(x, stream.Length)).Subscribe(p));
                    var result = await destination.SetData(stream, cancellationToken);
                    subscription.Execute(d => d.Dispose());
                    return result;
                });
    }

    private static PositionReportingStream CreateCompatibleStream(Stream original, FileProperties pr)
    {
        if (original.CanSeek)
        {
            return new PositionReportingStream(original);
        }

        return new PositionReportingStream(new AlwaysForwardStream(original, pr.Length));
    }

    public static IZafiroFile Mirror(this IZafiroFile file, Path root, IZafiroDirectory destinationRoot)
    {
        var relativeToRoot = file.Path.MakeRelativeTo(root);
        var translatedPath = destinationRoot.Path.Combine(relativeToRoot);
        var equivalentIn = destinationRoot.FileSystem.GetFile(translatedPath);
        return equivalentIn;
    }

    public static Task<Result<bool>> AreEqual(this IZafiroFile one, IZafiroFile two, IFileCompareStrategy strategy)
    {
        return strategy.Compare(one, two);
    }
}