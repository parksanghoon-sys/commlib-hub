namespace CommLib.Domain.Configuration;

public sealed class RequestResponseOptions
{
    public int DefaultTimeoutMs { get; init; } = 2000;
    public int MaxPendingRequests { get; init; } = 100;
}
