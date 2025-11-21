namespace Zafiro.UI.Navigation.Sections;

public record SectionGroup(string Key, string FriendlyName)
{
    public static SectionGroup Ungrouped { get; } = new("Ungrouped", "Ungrouped");
}
