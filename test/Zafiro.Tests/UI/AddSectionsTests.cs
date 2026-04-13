using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.UI.Navigation;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.Tests.UI;

public class AddSectionsTests
{
    [Fact]
    public void Multiple_AddSections_calls_accumulate_sections()
    {
        var services = new ServiceCollection();

        services.AddSections(builder =>
        {
            builder.AddSection<DummyViewModelA>("A", "Section A");
        });

        services.AddSections(builder =>
        {
            builder.AddSection<DummyViewModelB>("B", "Section B");
        });

        var provider = services.BuildServiceProvider();
        var sections = provider.GetRequiredService<IEnumerable<ISection>>().ToList();

        sections.Select(s => s.Id).Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public void Single_AddSections_call_registers_all_sections()
    {
        var services = new ServiceCollection();

        services.AddSections(builder =>
        {
            builder.AddSection<DummyViewModelA>("X", "Section X");
            builder.AddSection<DummyViewModelB>("Y", "Section Y");
        });

        var provider = services.BuildServiceProvider();
        var sections = provider.GetRequiredService<IEnumerable<ISection>>().ToList();

        sections.Select(s => s.Id).Should().BeEquivalentTo(new[] { "X", "Y" });
    }

    [Fact]
    public void Empty_AddSections_call_does_not_wipe_previous_sections()
    {
        var services = new ServiceCollection();

        services.AddSections(builder =>
        {
            builder.AddSection<DummyViewModelA>("Real", "Real Section");
        });

        // Simulate an assembly with no sections (empty builder)
        services.AddSections(_ => { });

        var provider = services.BuildServiceProvider();
        var sections = provider.GetRequiredService<IEnumerable<ISection>>().ToList();

        sections.Should().ContainSingle(s => s.Id == "Real");
    }

    private class DummyViewModelA;
    private class DummyViewModelB;
}
