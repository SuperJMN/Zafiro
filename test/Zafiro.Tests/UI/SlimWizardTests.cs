using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ReactiveUI;
using Zafiro.UI.Commands;
using Zafiro.UI.Wizards.Slim;
using Zafiro.UI.Wizards.Slim.Builder;

namespace Zafiro.Tests.UI;

public class SlimWizardTests
{
    private static readonly IScheduler Scheduler = ImmediateScheduler.Instance;

    [Fact]
    public void Page_is_set_after_build()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .WithCommitFinalStep(Scheduler);

        Assert.NotNull(wizard.CurrentPage);
        Assert.NotNull(wizard.CurrentPage.Title);
        Assert.IsType<MyPage>(wizard.CurrentPage.Content);
    }

    [Fact]
    public void Step_without_next_command_throws()
    {
        var step = new WizardStep(
            StepKind.Normal,
            string.Empty,
            _ => new object(),
            _ => null,
            _ => Observable.Return(string.Empty));
        var steps = new List<IWizardStep> { step };

        var ex = Assert.Throws<InvalidOperationException>(() => { new SlimWizard<object>(steps, Scheduler); });
        Assert.Contains("Next command", ex.Message);
    }

    [Fact]
    public void Step_creating_null_page_throws()
    {
        var step = new WizardStep(
            StepKind.Normal,
            string.Empty,
            _ => null!,
            _ => ReactiveCommand.Create(() => Result.Success(new object())).Enhance(),
            _ => Observable.Return(string.Empty));
        var steps = new List<IWizardStep> { step };

        var ex = Assert.Throws<InvalidOperationException>(() => { new SlimWizard<object>(steps, Scheduler); });
        Assert.Contains("null page", ex.Message);
    }

    [Fact]
    public async Task Go_next_sets_correct_page()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Success("")).Enhance(), "")
            .WithCommitFinalStep(Scheduler);

        wizard.Next.TryExecute();
        await wizard
            .WhenAnyValue(x => x.CurrentPage)
            .Select(page => page.Content)
            .OfType<MyIntPage>()
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(2))
            .ToTask();

        Assert.NotNull(wizard.CurrentPage);
        Assert.NotNull(wizard.CurrentPage.Title);
        Assert.IsType<MyIntPage>(wizard.CurrentPage.Content);
    }

    [Fact]
    public void Finished_wizard_should_stay_on_final_page_on_multiple_next()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Success("")).Enhance(), "")
            .WithCommitFinalStep(Scheduler);

        // Tries to go next, but nothing should happen
        wizard.Next.TryExecute();
        wizard.Next.TryExecute();
        wizard.Next.TryExecute();
        wizard.Next.TryExecute();

        Assert.Equal(wizard.CurrentStepIndex, 1);
    }

    [Fact]
    public async Task Finished_wizard_should_notify_result()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Success("Finished!")).Enhance(), "")
            .WithCommitFinalStep(Scheduler);

        var result = "";
        wizard.Finished.Subscribe(value => result = value);
        await wizard.TypedNext.Execute().Timeout(TimeSpan.FromSeconds(2)).ToTask();
        await wizard.TypedNext.Execute().Timeout(TimeSpan.FromSeconds(2)).ToTask();
        await wizard.Finished.Take(1).Timeout(TimeSpan.FromSeconds(2)).ToTask();

        Assert.Equal("Finished!", result);
    }

    [Fact]
    public async Task Finished_wizard_cannot_go_next()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Success("")).Enhance(), "")
            .WithCommitFinalStep(Scheduler);

        await wizard.TypedNext.Execute().Timeout(TimeSpan.FromSeconds(2)).ToTask();
        await wizard.TypedNext.Execute().Timeout(TimeSpan.FromSeconds(2)).ToTask();
        await wizard.Finished.Take(1).Timeout(TimeSpan.FromSeconds(2)).ToTask();

        Assert.Equal(1, wizard.CurrentStepIndex);
    }

    [Fact]
    public void Initial_page_cannot_go_back()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Success("")).Enhance(), "")
            .WithCommitFinalStep(Scheduler);

        Observable.Return(Unit.Default).InvokeCommand(wizard.Back);

        Assert.Equal(wizard.CurrentStepIndex, 0);
    }

    [Fact]
    public void Page_failure_cannot_go_next()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), _ => ReactiveCommand.Create(() => Result.Failure<int>("Error")).Enhance(), "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Failure<string>("Error")).Enhance(), "")
            .WithCommitFinalStep(Scheduler);

        wizard.Next.TryExecute();

        Assert.NotNull(wizard.CurrentPage);
        Assert.NotNull(wizard.CurrentPage.Title);
        Assert.Equal(wizard.CurrentStepIndex, 0);
        Assert.IsType<MyPage>(wizard.CurrentPage.Content);
    }

    [Fact]
    public void Page_go_next_and_back()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Success("")).Enhance(), "")
            .WithCommitFinalStep(Scheduler);

        wizard.Next.TryExecute();
        wizard.Back.TryExecute();

        Assert.NotNull(wizard.CurrentPage);
        Assert.NotNull(wizard.CurrentPage.Title);
        Assert.IsType<MyPage>(wizard.CurrentPage.Content);
    }
    
    [Fact]
    public async Task Page_completion_cannot_go_back()
    {
        var wizard = WizardBuilder
            .StartWith(() => new MyPage(), page => page.DoSomething, "")
            .Then(i => new MyIntPage(i), _ => ReactiveCommand.Create(() => Result.Success("")).Enhance(), "")
            .WithCompletionFinalStep(Scheduler);

        wizard.Next.TryExecute();
        await wizard
            .WhenAnyValue(x => x.CurrentPage)
            .Select(page => page.Content)
            .OfType<MyIntPage>()
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(2))
            .ToTask();
        wizard.Back.TryExecute();

        Assert.NotNull(wizard.CurrentPage);
        Assert.NotNull(wizard.CurrentPage.Title);
        Assert.IsType<MyIntPage>(wizard.CurrentPage.Content);
    }
}

public static class ReactiveCommandExtensions
{
    public static IDisposable TryExecute(
        this IEnhancedCommand command)
    {
        if (command.CanExecute(null))
            command.Execute(null);

        return Disposable.Empty;
    }
}
