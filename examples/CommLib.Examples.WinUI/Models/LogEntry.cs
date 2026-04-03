namespace CommLib.Examples.WinUI.Models;

public sealed record LogEntry(DateTimeOffset Timestamp, LogSeverity Severity, string Title, string Message)
{
    public string TimestampText => Timestamp.ToLocalTime().ToString("HH:mm:ss");

    public string SeverityText => Severity.ToString().ToUpperInvariant();

    public override string ToString()
    {
        return $"[{TimestampText}] {SeverityText} {Title}: {Message}";
    }
}
