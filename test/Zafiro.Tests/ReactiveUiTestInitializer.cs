using System.Runtime.CompilerServices;
using ReactiveUI.Builder;

namespace Zafiro.Tests;

internal static class ReactiveUiTestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }
}
