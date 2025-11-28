using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.SourceGenerators;
using Zafiro.Reactive;
using Zafiro.UI.Navigation;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

[PublicAPI]
public partial class Shell : ReactiveObject, IShell
{
    private readonly CompositeDisposable disposables = new();
    private readonly IServiceProvider provider;

    [Reactive] private INavigator navigator;
    [Reactive] private INavigationRoot? selectedSection;

    public Shell(ShellProperties shellProperties, IEnumerable<INavigationRoot> sections, IServiceProvider provider)
    {
        this.provider = provider;
        Sections = sections;

        ContentHeader = this.WhenAnyValue(x => x.Navigator)
            .WhereNotNull()
            .Select(nav => shellProperties.GetHeader(nav))
            .Switch()
            .ReplayLastActive();

        this.WhenAnyValue(x => x.SelectedSection)
            .WhereNotNull()
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(section => Navigator = section.Navigator)
            .DisposeWith(disposables);

        var sectionActions = provider.GetService<ISectionActions>();
        if (sectionActions is not null)
        {
            sectionActions.GoToSectionRequests
                .Subscribe(GoToSection)
                .DisposeWith(disposables);
        }

        SelectedSection = Sections.FirstOrDefault();
        Navigator = SelectedSection?.Navigator ?? new Navigator(provider, Maybe<ILogger>.None, RxApp.MainThreadScheduler);
        Header = shellProperties.Header;
    }

    public object Header { get; set; }
    public IObservable<object?> ContentHeader { get; }
    public IEnumerable<INavigationRoot> Sections { get; }

    public void GoToSection(string sectionName)
    {
        SelectedSection = Sections.First(x => x.Name == sectionName);
    }
}