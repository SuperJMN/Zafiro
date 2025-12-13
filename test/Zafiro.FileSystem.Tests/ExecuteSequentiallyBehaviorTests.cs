using CSharpFunctionalExtensions;
using FluentAssertions;
using Xunit;
using Zafiro.CSharpFunctionalExtensions;

namespace Zafiro.FileSystem.Tests;

public class ExecuteSequentiallyBehaviorTests
{
    [Fact]
    public async Task Does_ExecuteSequentially_stop_on_failure()
    {
        var sideEffect = false;

        var functions = new List<Func<Task<Result>>>
        {
            () => Task.FromResult(Result.Failure("Fail")),
            () =>
            {
                sideEffect = true;
                return Task.FromResult(Result.Success());
            }
        };

        var result = await functions.ExecuteSequentially();

        // If good design (fail-fast), sideEffect will be false.
        sideEffect.Should().BeFalse("because the new implementation should stop on first failure");
    }
}
