namespace Zafiro.UI;

public class SectionOld : ISectionOld
{
    public SectionOld(string title, object content, object? icon)
    {
        Title = title;
        Content = content;
        Icon = icon;
    }

    public string Title { get; }
    public object Icon { get; }
    public object Content { get; }
}