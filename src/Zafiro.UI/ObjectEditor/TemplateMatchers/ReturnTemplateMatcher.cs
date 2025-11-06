using System.Reflection;

namespace Zafiro.UI.ObjectEditor.TemplateMatchers;

internal class ReturnTemplateMatcher<T> : TemplateMatcher<T> where T : class
{
    private readonly Func<T> getTemplate;

    public ReturnTemplateMatcher(Func<T> getTemplate)
    {
        this.getTemplate = getTemplate;
    }

    protected override T SelectOverride(EditorCollection<T> editors, PropertyInfo property)
    {
        return getTemplate();
    }
}