using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.SourceGenerators;
using Zafiro.CSharpFunctionalExtensions;
using Zafiro.Reactive;
using Zafiro.UI.Navigation;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

[PublicAPI]
public partial class Shell : ReactiveObject, IShell
{
    private readonly CompositeDisposable disposables = new();
    private readonly IServiceProvider provider;
    private readonly ISectionSessionFactory sectionSessionFactory;
    private readonly Dictionary<ISection, Lazy<Task<Result<SectionScope>>>> sessions = new();

    [Reactive] private INavigator navigator;
    [Reactive] private ISection? selectedSection;

    public Shell(ShellProperties shellProperties, IEnumerable<ISection> sections, IServiceProvider provider)
    {
        this.provider = provider;
        sectionSessionFactory = provider.GetService<ISectionSessionFactory>() ?? new SectionSessionFactory(provider);
        Sections = sections;

        // Header follows the current navigator
        ContentHeader = this.WhenAnyValue(x => x.Navigator)
            .WhereNotNull()
            .Select(nav => shellProperties.GetHeader(nav))
            .Switch()
            .ReplayLastActive();

        // When section changes, create or get its session via async factory and bind Navigator reactively
        this.WhenAnyValue(x => x.SelectedSection)
            .WhereNotNull()
            .DistinctUntilChanged()
            .Select(section => GetOrCreate(section).ToObservable())
            .Switch()
            .Successes()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(session => Navigator = session.Navigator)
            .DisposeWith(disposables);

        // Listen to section change requests, if the action bus is registered
        var sectionActions = provider.GetService<ISectionActions>();
        if (sectionActions is not null)
        {
            sectionActions.GoToSectionRequests
                .Subscribe(GoToSection)
                .DisposeWith(disposables);
        }

        SelectedSection = Sections.FirstOrDefault();
        Header = shellProperties.Header;
    }

    public object Header { get; set; }
    public IObservable<object?> ContentHeader { get; }
    public IEnumerable<ISection> Sections { get; }

    public void GoToSection(string sectionName)
    {
        SelectedSection = Sections.First(x => x.Name == sectionName);
    }

    private Task<Result<SectionScope>> GetOrCreate(ISection section)
    {
        if (!sessions.TryGetValue(section, out var lazyTask))
        {
            lazyTask = new Lazy<Task<Result<SectionScope>>>(() => sectionSessionFactory.Create(section));
            sessions[section] = lazyTask;
        }

        return lazyTask.Value;
    }
}