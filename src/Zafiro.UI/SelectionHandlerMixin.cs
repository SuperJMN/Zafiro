namespace Zafiro.UI;

public static class SelectionHandlerMixin
{
    public static IObservable<SelectionKind> Kinds(this ISelectionHandler selectionHandler)
    {
        return selectionHandler.SelectionCount.WithLatestFrom(selectionHandler.TotalCount, (selected, total) => selected == 0 ? SelectionKind.None : total == selected ? SelectionKind.Full : SelectionKind.Partial);
    }
}