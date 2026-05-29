using System.Reactive.Disposables;
using JetBrains.Annotations;
using Zafiro.UI.Navigation.Sections;

namespace Zafiro.UI.Shell;

[PublicAPI]
public class Shell : IShell, IDisposable
{
    private readonly Dictionary<string, ISection> sectionsById;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ISection>> childrenByParentId;
    private readonly Dictionary<string, string> selectedChildByParentId = new();
    private IReadOnlyList<SectionLevel> childLevels = [];

    public Shell(IEnumerable<ISection> sections, IServiceProvider provider)
    {
        Sections = sections.ToList();
        sectionsById = Sections.ToDictionary(section => section.Id);
        childrenByParentId = Sections
            .Where(section => !string.IsNullOrWhiteSpace(section.ParentId))
            .GroupBy(section => section.ParentId!)
            .ToDictionary(group => group.Key, group => Sort(group).ToList() as IReadOnlyList<ISection>);

        var rootSections = Sort(Sections.Where(section => string.IsNullOrWhiteSpace(section.ParentId))).ToList();
        var initialRoot = rootSections.FirstOrDefault(section => section.IsVisible);
        var initialSection = initialRoot is null ? null : ResolveSelectedSection(initialRoot);

        SelectedSection = new global::Reactive.Bindings.ReactiveProperty<ISection>(initialSection!);
        SelectedPath = new global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<ISection>>(initialSection is null ? [] : BuildPath(initialSection));
        ChildLevels = new global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<SectionLevel>>([]);
        RootLevel = new SectionLevel(rootSections, initialRoot!, SelectSection);

        if (initialSection is not null)
        {
            UpdateSelectedSection(initialSection);
        }
    }

    public void Dispose()
    {
        RootLevel.Dispose();
        DisposeChildLevels();
        SelectedSection.Dispose();
        SelectedPath.Dispose();
        ChildLevels.Dispose();

        foreach (var section in Sections)
        {
            section.Dispose();
        }
    }

    public IEnumerable<ISection> Sections { get; }
    public SectionLevel RootLevel { get; }
    public global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<SectionLevel>> ChildLevels { get; }
    public global::Reactive.Bindings.ReactiveProperty<ISection> SelectedSection { get; }
    public global::Reactive.Bindings.ReactiveProperty<IReadOnlyList<ISection>> SelectedPath { get; }

    public void GoToSection(string sectionId)
    {
        SelectSection(sectionsById[sectionId]);
    }

    private static IEnumerable<ISection> Sort(IEnumerable<ISection> sections)
    {
        return sections
            .OrderBy(section => section.SortOrder);
    }

    private IReadOnlyList<ISection> GetChildren(ISection parent)
    {
        return childrenByParentId.TryGetValue(parent.Id, out var children)
            ? Sort(children).ToList()
            : [];
    }

    private ISection ResolveSelectedSection(ISection section)
    {
        var children = GetChildren(section)
            .Where(child => child.IsVisible)
            .ToList();
        if (children.Count is 0)
        {
            return section;
        }

        var rememberedChild = selectedChildByParentId.TryGetValue(section.Id, out var childId)
            && sectionsById.TryGetValue(childId, out var child)
            && children.Contains(child)
                ? child
                : children[0];

        return ResolveSelectedSection(rememberedChild);
    }

    private void SelectSection(ISection section)
    {
        UpdateSelectedSection(ResolveSelectedSection(section));
    }

    private void UpdateSelectedSection(ISection section)
    {
        var path = BuildPath(section);
        RememberSelectedChildren(path);

        SelectedSection.Value = section;
        SelectedPath.Value = path;
        RootLevel.SetSelectedSection(path[0]);
        ReplaceChildLevels(CreateChildLevels(path));
    }

    private IReadOnlyList<ISection> BuildPath(ISection section)
    {
        var path = new Stack<ISection>();
        var current = section;

        while (true)
        {
            path.Push(current);

            if (string.IsNullOrWhiteSpace(current.ParentId) || !sectionsById.TryGetValue(current.ParentId, out var parent))
            {
                break;
            }

            current = parent;
        }

        return path.ToList();
    }

    private void RememberSelectedChildren(IReadOnlyList<ISection> path)
    {
        foreach (var pair in path.Zip(path.Skip(1)))
        {
            selectedChildByParentId[pair.First.Id] = pair.Second.Id;
        }
    }

    private IReadOnlyList<SectionLevel> CreateChildLevels(IReadOnlyList<ISection> path)
    {
        return path
            .Take(path.Count - 1)
            .Select((section, index) => new SectionLevel(GetChildren(section), path[index + 1], SelectSection))
            .ToList();
    }

    private void ReplaceChildLevels(IReadOnlyList<SectionLevel> levels)
    {
        DisposeChildLevels();
        childLevels = levels;
        ChildLevels.Value = childLevels;
    }

    private void DisposeChildLevels()
    {
        foreach (var level in childLevels)
        {
            level.Dispose();
        }
    }
}
