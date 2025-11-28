using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;

namespace Zafiro.UI.Navigation;

public class Navigator<TInitial> : Navigator where TInitial : class
{
    public Navigator(IServiceProvider serviceProvider, Maybe<ILogger> logger, IScheduler? scheduler)
        : base(serviceProvider, logger, scheduler)
    {
        SetInitialLoader(() => NavigateUsingFactory(() => serviceProvider.GetRequiredService<TInitial>()));
    }
}