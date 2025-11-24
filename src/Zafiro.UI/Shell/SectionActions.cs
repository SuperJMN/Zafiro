using System.Reactive.Subjects;

namespace Zafiro.UI.Shell;

public sealed class SectionActions : ISectionActions
{
    private readonly Subject<string> subject = new();

    public IObservable<string> GoToSectionRequests => subject.AsObservable();

    public void RequestGoToSection(string sectionName) => subject.OnNext(sectionName);
}