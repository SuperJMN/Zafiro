namespace Zafiro.ProgressReporting;

public sealed record NotStarted : Progress
{
    public static readonly NotStarted Instance = new();
}

public sealed record Completed : Progress
{
    public static readonly Completed Instance = new();
}