namespace Zafiro.UI.Navigation.Sections;

public interface ISection
{
    bool IsVisible { get; set; }
    int SortOrder { get; set; }
}