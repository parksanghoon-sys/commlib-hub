namespace CommLib.Examples.WinUI.Models;

/// <summary>
/// LogEntry 레코드입니다.
/// </summary>
public sealed record LogEntry(DateTimeOffset Timestamp, LogSeverity Severity, string Title, string Message)
{
    /// <summary>
    /// TimestampText 값을 가져옵니다.
    /// </summary>
    public string TimestampText => Timestamp.ToLocalTime().ToString("HH:mm:ss");

    /// <summary>
    /// SeverityText 값을 가져옵니다.
    /// </summary>
    public string SeverityText => Severity.ToString().ToUpperInvariant();

    /// <summary>
    /// ToString 작업을 수행합니다.
    /// </summary>
    public override string ToString()
    {
        return $"[{TimestampText}] {SeverityText} {Title}: {Message}";
    }
}