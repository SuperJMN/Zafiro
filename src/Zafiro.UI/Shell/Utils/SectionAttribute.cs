namespace Zafiro.UI.Shell.Utils;

[AttributeUsage(AttributeTargets.Class)]
public class SectionAttribute(string? name = null, string? icon = null, int sortIndex = 0, Type? contractType = null) : Attribute
{
    /// <summary>
    /// Internal key for the section. Defaults to the view model name if not provided.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Friendly display name for the section. Falls back to <see cref="Name"/> when not set.
    /// </summary>
    public string? FriendlyName { get; set; }

    public string? Icon { get; } = icon;
    public int SortIndex { get; } = sortIndex;

    /// <summary>
    /// Optional service contract to resolve as the initial content for this section.
    /// If null, the annotated class type will be used.
    /// </summary>
    public Type? ContractType { get; } = contractType;
}
