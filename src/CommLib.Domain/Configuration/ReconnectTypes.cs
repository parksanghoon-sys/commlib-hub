namespace CommLib.Domain.Configuration;

public static class ReconnectTypes
{
    public const string None = "None";
    public const string Linear = "Linear";
    public const string Exponential = "Exponential";
    public const string Backoff = "Backoff";
    public const string ExponentialBackoff = "ExponentialBackoff";
}
