namespace Zafiro.UI.Navigation.Sections;

public class SectionGroupHeader(string title) : Section, ISectionGroupHeader
{
    public string Title { get; } = title;
}