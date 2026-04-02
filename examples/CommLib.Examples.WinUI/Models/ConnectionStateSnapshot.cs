namespace CommLib.Examples.WinUI.Models;

public sealed record ConnectionStateSnapshot(bool IsConnected, string StatusText, string StatusDetail);
