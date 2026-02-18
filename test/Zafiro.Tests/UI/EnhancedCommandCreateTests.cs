using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Zafiro.UI.Commands;

namespace Zafiro.Tests.UI;

public class EnhancedCommandCreateTests
{
    // --- Action (no return value) ---

    [Fact]
    public async Task Create_Action_ExecutesSuccessfully()
    {
        var executed = false;
        var cmd = EnhancedCommand.Create(() => executed = true);

        await cmd.Execute().FirstAsync();

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Create_Action_ReturnsUnit()
    {
        var cmd = EnhancedCommand.Create(() => { });

        var result = await cmd.Execute().FirstAsync();

        result.Should().Be(Unit.Default);
    }

    [Fact]
    public void Create_Action_SetsTextAndName()
    {
        var cmd = EnhancedCommand.Create(() => { }, text: "Click me", name: "ClickCommand");

        cmd.Text.Should().Be("Click me");
        cmd.Name.Should().Be("ClickCommand");
    }

    [Fact]
    public void Create_Action_RespectsCanExecute()
    {
        var canExecute = new BehaviorSubject<bool>(false);
        var cmd = EnhancedCommand.Create(() => { }, canExecute);

        ((System.Windows.Input.ICommand)cmd).CanExecute(null).Should().BeFalse();

        canExecute.OnNext(true);
        ((System.Windows.Input.ICommand)cmd).CanExecute(null).Should().BeTrue();
    }

    // --- Func<Task> (async action) ---

    [Fact]
    public async Task Create_FuncTask_ExecutesSuccessfully()
    {
        var executed = false;
        var cmd = EnhancedCommand.Create(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        await cmd.Execute().FirstAsync();

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Create_FuncTask_ReturnsUnit()
    {
        var cmd = EnhancedCommand.Create(async () => await Task.Delay(1));

        var result = await cmd.Execute().FirstAsync();

        result.Should().Be(Unit.Default);
    }

    // --- CreateWithResult<T> (Func<T>) ---

    [Fact]
    public async Task CreateWithResult_Func_ReturnsValue()
    {
        var cmd = EnhancedCommand.CreateWithResult(() => 42);

        var result = await cmd.Execute().FirstAsync();

        result.Should().Be(42);
    }

    [Fact]
    public void CreateWithResult_Func_SetsTextAndName()
    {
        var cmd = EnhancedCommand.CreateWithResult(() => "hello", text: "Get greeting", name: "Greeting");

        cmd.Text.Should().Be("Get greeting");
        cmd.Name.Should().Be("Greeting");
    }

    // --- CreateWithResult<T> (Func<Task<T>>) ---

    [Fact]
    public async Task CreateWithResult_FuncTaskT_ReturnsValue()
    {
        var cmd = EnhancedCommand.CreateWithResult(async () =>
        {
            await Task.Delay(1);
            return 99;
        });

        var result = await cmd.Execute().FirstAsync();

        result.Should().Be(99);
    }

    // --- CreateWithResult (Func<Result> / Func<Task<Result>>) ---

    [Fact]
    public async Task CreateWithResult_Result_Success()
    {
        var cmd = EnhancedCommand.CreateWithResult(() => Result.Success());

        var result = await cmd.Execute().FirstAsync();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWithResult_FuncTaskResult_Failure()
    {
        var cmd = EnhancedCommand.CreateWithResult(async () =>
        {
            await Task.Delay(1);
            return Result.Failure("error");
        });

        var result = await cmd.Execute().FirstAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error");
    }

    // --- Create<TIn, TOut> (with input parameter) ---

    [Fact]
    public async Task Create_FuncWithParameter_ExecutesWithInput()
    {
        var cmd = EnhancedCommand.Create((int x) => x * 2);

        var result = await cmd.Execute(5).FirstAsync();

        result.Should().Be(10);
    }

    [Fact]
    public async Task Create_AsyncFuncWithParameter_ExecutesWithInput()
    {
        var cmd = EnhancedCommand.Create(async (string s) =>
        {
            await Task.Delay(1);
            return s.ToUpper();
        });

        var result = await cmd.Execute("hello").FirstAsync();

        result.Should().Be("HELLO");
    }

    // --- Create<TIn> (Action<TIn>) ---

    [Fact]
    public async Task Create_ActionWithParameter_Executes()
    {
        var received = "";
        var cmd = EnhancedCommand.Create((string s) => received = s);

        await cmd.Execute("test").FirstAsync();

        received.Should().Be("test");
    }

    // --- Create<TIn> (Func<TIn, Task>) ---

    [Fact]
    public async Task Create_AsyncActionWithParameter_Executes()
    {
        var received = 0;
        var cmd = EnhancedCommand.Create(async (int x) =>
        {
            await Task.Delay(1);
            received = x;
        });

        await cmd.Execute(42).FirstAsync();

        received.Should().Be(42);
    }

    // --- Existing Result<T> variants still work ---

    [Fact]
    public async Task Create_ResultT_Success()
    {
        var cmd = EnhancedCommand.Create(() => Result.Success(100));

        var result = await cmd.Execute().FirstAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(100);
    }

    [Fact]
    public async Task Create_ResultT_Failure()
    {
        var cmd = EnhancedCommand.Create(() => Result.Failure<int>("error"));

        var result = await cmd.Execute().FirstAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error");
    }

    [Fact]
    public async Task Create_AsyncResultT_Success()
    {
        var cmd = EnhancedCommand.Create(async () =>
        {
            await Task.Delay(1);
            return Result.Success("ok");
        });

        var result = await cmd.Execute().FirstAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }
}
