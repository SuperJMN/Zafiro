using Zafiro.UI.ObjectEditor.TemplateMatchers;

namespace Zafiro.UI.ObjectEditor;

public interface IObjectEditor<TEditor, TTemplate>
{
    object SelectedItems { get; set; }
    TTemplate DefaultEditorTemplate { get; set; }
    EditorCollection<TTemplate> EditorsCore { get; }
}