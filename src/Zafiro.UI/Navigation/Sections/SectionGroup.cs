namespace Zafiro.UI.Navigation.Sections;

public class SectionGroup(string key, string? friendlyName) : ValueObject<SectionGroup>
{
    /// <summary>
    /// Default group for sections that don't have an explicit group.
    /// Uses a reserved key to avoid collisions with user-defined groups.
    /// FriendlyName is null so no header is displayed.
    /// </summary>
    public static readonly SectionGroup Default = new("__zafiro_default_group__", null);

    public string? FriendlyName { get; } = friendlyName;

    public string Key { get; } = key;

    protected override bool EqualsCore(SectionGroup other)
    {
        return Key == other.Key;
    }

    protected override int GetHashCodeCore()
    {
        return Key.GetHashCode();
    }
}
