namespace CommLib.Domain.Configuration;

public sealed class ReconnectOptions
{
    public string Type { get; init; } = "None";
    public int MaxAttempts { get; init; }
    public int BaseDelayMs { get; init; } = 500;
    public int MaxDelayMs { get; init; } = 10000;
    public int IntervalMs { get; init; } = 3000;
}
