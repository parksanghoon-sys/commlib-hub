namespace CommLib.Examples.WinUI.Models;

/// <summary>
/// ConnectionStateSnapshot 레코드입니다.
/// </summary>
public sealed record ConnectionStateSnapshot(bool IsConnected, string StatusText, string StatusDetail);