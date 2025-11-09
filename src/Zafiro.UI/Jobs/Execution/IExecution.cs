using Zafiro.ProgressReporting;

namespace Zafiro.UI.Jobs.Execution;

public interface IExecution
{
    public ReactiveCommandBase<Unit, Unit> Start { get; }
    public ReactiveCommandBase<Unit, Unit>? Stop { get; }
    public IObservable<Progress> Progress { get; }
}