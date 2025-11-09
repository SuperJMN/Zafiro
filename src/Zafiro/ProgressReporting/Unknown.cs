namespace Zafiro.ProgressReporting;

public sealed record Unknown : Progress
{
    public static readonly Unknown Instance = new();
}