using System.ComponentModel;

namespace Zafiro.UI.Navigation.Sections;

public interface ISection : INotifyPropertyChanged
{
    bool IsVisible { get; set; }
    int SortOrder { get; set; }

    string Name { get; }
    string FriendlyName { get; }
    SectionGroup Group { get; }
    object? Icon { get; }
    IObservable<object> Content { get; }
}
