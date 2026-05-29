using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Zafiro.UI.Navigation;
using Zafiro.UI.Shell;
using Zafiro.UI.Shell.Utils;

namespace Zafiro.Tests.UI;

public class AttributeShellRegistrationTests
{
    [Fact]
    public async Task Filtered_attribute_registration_creates_hierarchical_sections_with_scoped_navigation()
    {
        var services = new ServiceCollection();
        services.AddZafiroShell();
        services.AddTransient<ActiveUsersDetailsViewModel>();
        services.AddSectionsFromAttributes(typeof(AttributeShellRegistrationTests).Assembly, IsDemoSection);

        using var provider = services.BuildServiceProvider();
        using var shell = (Shell)provider.GetRequiredService<IShell>();

        shell.RootLevel.Sections.Select(section => section.Id).Should().Equal("workspace");
        shell.SelectedPath.Value.Select(section => section.Id).Should().Equal("workspace", "users", "active-users");
        shell.ChildLevels.Value.Should().HaveCount(2);
        shell.ChildLevels.Value[0].Sections.Select(section => section.Id).Should().Equal("users", "security");
        shell.ChildLevels.Value[1].Sections.Select(section => section.Id).Should().Equal("active-users", "user-roles");

        var activeUsersNavigator = shell.SelectedSection.Value.Navigator;
        await activeUsersNavigator.Go(typeof(ActiveUsersDetailsViewModel));

        shell.GoToSection("security");
        var securityContent = await shell.SelectedSection.Value.Navigator.Content.FirstAsync();
        securityContent.Should().BeOfType<SecurityViewModel>();

        shell.GoToSection("active-users");
        var activeUsersContent = await shell.SelectedSection.Value.Navigator.Content.FirstAsync();
        activeUsersContent.Should().BeOfType<ActiveUsersDetailsViewModel>();
    }

    private static bool IsDemoSection(Type type)
    {
        return type.DeclaringType == typeof(AttributeShellRegistrationTests);
    }

    [Section("workspace", "mdi-view-dashboard", 0)]
    public class WorkspaceViewModel;

    [Section("users", "mdi-account-group", 0, ParentId = "workspace")]
    public class UsersViewModel;

    [Section("security", "mdi-shield-key", 1, ParentId = "workspace")]
    public class SecurityViewModel;

    [Section("active-users", "mdi-account-check", 0, ParentId = "users")]
    public class ActiveUsersViewModel;

    [Section("user-roles", "mdi-account-key", 1, ParentId = "users")]
    public class UserRolesViewModel;

    public class ActiveUsersDetailsViewModel;
}
