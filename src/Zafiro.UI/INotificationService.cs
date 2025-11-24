namespace Zafiro.UI;

public interface INotificationService
{
    Task Show(string message, Maybe<string> title);
}