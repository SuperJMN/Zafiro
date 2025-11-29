using System.Reactive.Disposables;
using JetBrains.Annotations;
using Reactive.Bindings;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

[PublicAPI]
public class Shell : IShell
{
    private readonly CompositeDisposable disposables = new();
    private readonly IServiceProvider provider;


    public Shell(ShellProperties shellProperties, IEnumerable<INavigationRoot> sections, IServiceProvider provider)
    {
        this.provider = provider;
        Sections = sections;
        SelectedSection = new global::Reactive.Bindings.ReactiveProperty<INavigationRoot>(Sections.FirstOrDefault());
        SelectedSection.Subscribe(root => { });

        // ContentHeader = this.WhenAnyValue(x => x.Navigator)
        //     .WhereNotNull()
        //     .Select(nav => shellProperties.GetHeader(nav))
        //     .Switch()
        //     .ReplayLastActive();

        // this.WhenAnyValue(x => x.SelectedSection)
        //     .WhereNotNull()
        //     .DistinctUntilChanged()
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .Subscribe(section => Navigator = section.Navigator)
        //     .DisposeWith(disposables);
        //
        // var sectionActions = provider.GetService<ISectionActions>();
        // if (sectionActions is not null)
        // {
        //     sectionActions.GoToSectionRequests
        //         .Subscribe(GoToSection)
        //         .DisposeWith(disposables);
        // }
        //
        // SelectedSection = Sections.FirstOrDefault();
        // Navigator = SelectedSection?.Navigator ?? new Navigator(provider, Maybe<ILogger>.None, RxApp.MainThreadScheduler);
        // Header = shellProperties.Header;
    }

    public object Header { get; set; }
    public ReadOnlyReactiveProperty<object?> ContentHeader { get; }
    public IEnumerable<INavigationRoot> Sections { get; }
    public global::Reactive.Bindings.ReactiveProperty<INavigationRoot> SelectedSection { get; }

    public void GoToSection(string sectionName)
    {
        SelectedSection.Value = Sections.First(x => x.Name == sectionName);
    }
}