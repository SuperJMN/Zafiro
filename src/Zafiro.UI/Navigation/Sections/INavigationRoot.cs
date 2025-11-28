using System.ComponentModel;

namespace Zafiro.UI.Navigation.Sections;

public interface INavigationRoot : INotifyPropertyChanged
{
    bool IsVisible { get; set; }
    int SortOrder { get; set; }
    string Name { get; }
    string FriendlyName { get; }
    SectionGroup Group { get; }
    object? Icon { get; }
    INavigator Navigator { get; }
}