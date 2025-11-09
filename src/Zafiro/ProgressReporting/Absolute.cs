namespace Zafiro.ProgressReporting;

public sealed record Absolute(double Current, double Total) : Progress;