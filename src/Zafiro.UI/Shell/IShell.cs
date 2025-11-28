using Zafiro.UI.Navigation;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

public interface IShell
{
    public object Header { get; set; }
    public IObservable<object?> ContentHeader { get; }
    IEnumerable<INavigationRoot> Sections { get; }
    INavigationRoot SelectedSection { get; set; }
    INavigator Navigator { get; }
    void GoToSection(string sectionName);
}