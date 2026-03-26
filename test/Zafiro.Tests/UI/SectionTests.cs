using Microsoft.Extensions.DependencyInjection;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.Tests.UI;

public class SectionTests
{
    private static Section CreateSection(string id = "section-id", string? friendlyName = null)
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        return new Section(id, provider, typeof(object), friendlyName: friendlyName);
    }

    [Fact]
    public void ShortName_returns_explicit_value_when_present()
    {
        using var section = CreateSection(friendlyName: "Friendly name");

        section.ShortName = "Short";

        section.ShortName.Should().Be("Short");
    }

    [Fact]
    public void ShortName_falls_back_to_friendly_name_when_not_set()
    {
        using var section = CreateSection(friendlyName: "Friendly name");

        section.ShortName.Should().Be("Friendly name");
    }

    [Fact]
    public void ShortName_falls_back_to_id_when_friendly_name_is_empty()
    {
        using var section = CreateSection(friendlyName: "");

        section.ShortName.Should().Be("section-id");
    }
}
