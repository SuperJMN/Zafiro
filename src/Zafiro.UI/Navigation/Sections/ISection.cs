using System.ComponentModel;

namespace Zafiro.UI.Navigation.Sections;

public interface ISection : INotifyPropertyChanged, IDisposable
{
    bool IsVisible { get; set; }
    int SortOrder { get; set; }
    string Id { get; }
    public string? ShortName { get; }
    string FriendlyName { get; }
    SectionGroup Group { get; }
    object? Icon { get; }
    INavigator Navigator { get; }
    
}