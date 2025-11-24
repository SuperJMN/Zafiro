using CSharpFunctionalExtensions;
using Serilog;

namespace Zafiro.FileSystem.Core;

public static class ExceptionHandler
{
    public static string HandleError(Path path, Exception exception, Maybe<ILogger> logger)
    {
        if (exception is TaskCanceledException)
        {
            return "Cancelled";
        }

        logger.Execute(l => l.Error(exception, "Error while accessing {Path}", path));
        return $"Error while accessing {path}. Details: {exception}";
    }
}