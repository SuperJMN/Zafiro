using CSharpFunctionalExtensions;

namespace Zafiro.UI.Navigation.Sections;

public class SectionGroup(string key, string? friendlyName) : ValueObject<SectionGroup>
{
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