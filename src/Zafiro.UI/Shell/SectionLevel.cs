using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

public sealed class SectionLevel : IDisposable
{
    private readonly CompositeDisposable disposable = new();
    private readonly Action<ISection> selectSection;
    private bool isUpdatingSelection;

    public SectionLevel(IEnumerable<ISection> sections, ISection selectedSection, Action<ISection> selectSection)
    {
        Sections = sections.ToList();
        SelectedSection = new global::Reactive.Bindings.ReactiveProperty<ISection>(selectedSection).DisposeWith(disposable);
        this.selectSection = selectSection;

        SelectedSection
            .Skip(1)
            .Where(_ => !isUpdatingSelection)
            .Do(this.selectSection)
            .Subscribe()
            .DisposeWith(disposable);
    }

    public IReadOnlyList<ISection> Sections { get; }

    public global::Reactive.Bindings.ReactiveProperty<ISection> SelectedSection { get; }

    internal void SetSelectedSection(ISection selectedSection)
    {
        isUpdatingSelection = true;
        SelectedSection.Value = selectedSection;
        isUpdatingSelection = false;
    }

    public void Dispose()
    {
        disposable.Dispose();
    }
}
