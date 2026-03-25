namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents the retry policy used when reconnecting to a device.
/// </summary>
public sealed class ReconnectOptions
{
    /// <summary>
    /// Gets the reconnect strategy name.
    /// </summary>
    public string Type { get; init; } = "None";
    /// <summary>
    /// Gets the maximum number of reconnect attempts.
    /// </summary>
    public int MaxAttempts { get; init; }
    /// <summary>
    /// Gets the base delay in milliseconds used by backoff-based strategies.
    /// </summary>
    public int BaseDelayMs { get; init; } = 500;
    /// <summary>
    /// Gets the maximum delay in milliseconds for retry backoff.
    /// </summary>
    public int MaxDelayMs { get; init; } = 10000;
    /// <summary>
    /// Gets the fixed reconnect interval in milliseconds for interval-based strategies.
    /// </summary>
    public int IntervalMs { get; init; } = 3000;
}
