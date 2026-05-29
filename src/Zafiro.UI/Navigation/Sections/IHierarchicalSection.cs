namespace Zafiro.UI.Navigation.Sections;

public interface IHierarchicalSection : ISection
{
    string? ParentId { get; }
}
