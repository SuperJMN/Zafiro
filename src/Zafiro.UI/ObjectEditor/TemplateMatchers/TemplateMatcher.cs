using System.Reflection;

namespace Zafiro.UI.ObjectEditor.TemplateMatchers;

public abstract class TemplateMatcher<T> : ITemplateMatcher<T> where T : class
{
    public T Select(EditorCollection<T> editors, PropertyInfo property)
    {
        var selected = SelectOverride(editors, property);

        if (selected != null)
        {
            return selected;
        }

        return Next?.Select(editors, property);
    }

    public ITemplateMatcher<T> Next { get; set; }
    protected abstract T SelectOverride(EditorCollection<T> editors, PropertyInfo property);

    public ITemplateMatcher<T> Chain(ITemplateMatcher<T> nextlogger)
    {
        ITemplateMatcher<T> lastLogger = this;

        while (lastLogger.Next != null) lastLogger = lastLogger.Next;

        lastLogger.Next = nextlogger;
        return this;
    }
}