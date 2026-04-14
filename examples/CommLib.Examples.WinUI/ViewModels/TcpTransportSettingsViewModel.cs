using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// TcpTransportSettingsViewModel 타입입니다.
/// </summary>
public sealed class TcpTransportSettingsViewModel : TransportSettingsViewModel
{
    /// <summary>
    /// _host 값을 나타냅니다.
    /// </summary>
    private string _host = "127.0.0.1";
    /// <summary>
    /// _port 값을 나타냅니다.
    /// </summary>
    private string _port = "7001";
    /// <summary>
    /// _connectTimeoutMs 값을 나타냅니다.
    /// </summary>
    private string _connectTimeoutMs = "1000";
    /// <summary>
    /// _bufferSize 값을 나타냅니다.
    /// </summary>
    private string _bufferSize = "1024";
    /// <summary>
    /// _noDelay 값을 나타냅니다.
    /// </summary>
    private bool _noDelay = true;

    /// <summary>
    /// Host 값을 가져옵니다.
    /// </summary>
    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    /// <summary>
    /// Port 값을 가져옵니다.
    /// </summary>
    public string Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    /// <summary>
    /// ConnectTimeoutMs 값을 가져옵니다.
    /// </summary>
    public string ConnectTimeoutMs
    {
        get => _connectTimeoutMs;
        set => SetProperty(ref _connectTimeoutMs, value);
    }

    /// <summary>
    /// BufferSize 값을 가져옵니다.
    /// </summary>
    public string BufferSize
    {
        get => _bufferSize;
        set => SetProperty(ref _bufferSize, value);
    }

    /// <summary>
    /// NoDelay 값을 가져옵니다.
    /// </summary>
    public bool NoDelay
    {
        get => _noDelay;
        set => SetProperty(ref _noDelay, value);
    }

    /// <summary>
    /// Title 값을 가져옵니다.
    /// </summary>
    public override string Title => "TCP Target";

    /// <summary>
    /// Subtitle 값을 가져옵니다.
    /// </summary>
    public override string Subtitle => "Point-to-point socket session for controllers, PLCs, and simulators.";

    /// <summary>
    /// BuildTransportOptions 작업을 수행합니다.
    /// </summary>
    public override TransportOptions BuildTransportOptions()
    {
        return new TcpClientTransportOptions
        {
            Type = "TcpClient",
            Host = Host.Trim(),
            Port = ParseInt(Port, "TCP Port"),
            ConnectTimeoutMs = ParseInt(ConnectTimeoutMs, "TCP Connect Timeout"),
            BufferSize = ParseInt(BufferSize, "TCP Buffer Size"),
            NoDelay = NoDelay
        };
    }
}