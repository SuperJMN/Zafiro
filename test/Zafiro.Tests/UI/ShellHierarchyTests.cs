using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.UI.Navigation.Sections;
using Zafiro.UI.Shell;

namespace Zafiro.Tests.UI;

public class ShellHierarchyTests
{
    [Fact]
    public void Initial_selection_uses_first_visible_descendant_of_first_root()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var shell = new Shell([
            CreateSection(provider, "admin", sortOrder: 0),
            CreateSection(provider, "reports", sortOrder: 1),
            CreateSection(provider, "roles", parentId: "admin", sortOrder: 1),
            CreateSection(provider, "users", parentId: "admin", sortOrder: 0),
        ], provider);

        shell.RootLevel.Sections.Select(section => section.Id).Should().Equal("admin", "reports");
        shell.RootLevel.SelectedSection.Value.Id.Should().Be("admin");
        shell.SelectedSection.Value.Id.Should().Be("users");
        shell.SelectedPath.Value.Select(section => section.Id).Should().Equal("admin", "users");
        shell.ChildLevels.Value.Should().ContainSingle();
        shell.ChildLevels.Value[0].Sections.Select(section => section.Id).Should().Equal("users", "roles");
        shell.ChildLevels.Value[0].SelectedSection.Value.Id.Should().Be("users");
    }

    [Fact]
    public void Selecting_a_root_restores_the_last_selected_descendant_for_that_branch()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var shell = new Shell([
            CreateSection(provider, "admin", sortOrder: 0),
            CreateSection(provider, "reports", sortOrder: 1),
            CreateSection(provider, "users", parentId: "admin", sortOrder: 0),
            CreateSection(provider, "roles", parentId: "admin", sortOrder: 1),
            CreateSection(provider, "sales", parentId: "reports", sortOrder: 0),
        ], provider);

        shell.GoToSection("roles");
        shell.GoToSection("sales");
        shell.GoToSection("admin");

        shell.RootLevel.SelectedSection.Value.Id.Should().Be("admin");
        shell.SelectedSection.Value.Id.Should().Be("roles");
        shell.SelectedPath.Value.Select(section => section.Id).Should().Equal("admin", "roles");
        shell.ChildLevels.Value[0].SelectedSection.Value.Id.Should().Be("roles");
    }

    [Fact]
    public void Flat_sections_remain_supported()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var shell = new Shell([
            CreateSection(provider, "settings", sortOrder: 1),
            CreateSection(provider, "home", sortOrder: 0),
        ], provider);

        shell.RootLevel.Sections.Select(section => section.Id).Should().Equal("home", "settings");
        shell.RootLevel.SelectedSection.Value.Id.Should().Be("home");
        shell.SelectedSection.Value.Id.Should().Be("home");
        shell.SelectedPath.Value.Select(section => section.Id).Should().Equal("home");
        shell.ChildLevels.Value.Should().BeEmpty();
    }

    private static Section CreateSection(ServiceProvider provider, string id, string? parentId = null, int sortOrder = 0)
    {
        return new Section(id, provider, typeof(object), friendlyName: id, parentId: parentId)
        {
            SortOrder = sortOrder,
        };
    }
}
