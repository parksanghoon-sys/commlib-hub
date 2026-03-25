namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents request/response timing and capacity settings.
/// </summary>
public sealed class RequestResponseOptions
{
    /// <summary>
    /// Gets the default request timeout in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; init; } = 2000;
    /// <summary>
    /// Gets the maximum number of requests allowed to remain pending at once.
    /// </summary>
    public int MaxPendingRequests { get; init; } = 100;
}
