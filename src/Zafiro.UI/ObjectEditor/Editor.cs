namespace Zafiro.UI.ObjectEditor;

public class Editor<TTemplate>
{
    public Editor(TTemplate template, EditorKey key)
    {
        Template = template;
        Key = key;
    }

    public TTemplate Template { get; set; }
    public EditorKey Key { get; set; }
}