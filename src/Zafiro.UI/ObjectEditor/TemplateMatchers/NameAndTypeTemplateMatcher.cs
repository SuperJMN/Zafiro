using System.Reflection;

namespace Zafiro.UI.ObjectEditor.TemplateMatchers;

public class NameAndTypeTemplateMatcher<T> : TemplateMatcher<T> where T : class
{
    protected override T SelectOverride(EditorCollection<T> editors, PropertyInfo property)
    {
        var match = editors.FirstOrDefault(x => HasSameTypeAndName(property, x));

        return match?.Template;
    }

    private static bool HasSameTypeAndName(PropertyInfo property, Editor<T> x)
    {
        var sameType = x.Key.TargetType == property.PropertyType;
        var sameName = x.Key.Properties.Any(propName => string.Equals(propName, property.Name));
        return sameType && sameName;
    }
}