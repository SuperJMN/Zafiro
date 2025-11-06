using System.Reflection;

namespace Zafiro.UI.ObjectEditor.TemplateMatchers;

public interface ITemplateMatcher<T>
{
    ITemplateMatcher<T> Next { get; set; }
    T Select(EditorCollection<T> editors, PropertyInfo property);
}