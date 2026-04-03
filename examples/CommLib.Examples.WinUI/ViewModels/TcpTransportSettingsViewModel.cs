using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class TcpTransportSettingsViewModel : TransportSettingsViewModel
{
    private string _host = "127.0.0.1";
    private string _port = "7001";
    private string _connectTimeoutMs = "1000";
    private string _bufferSize = "1024";
    private bool _noDelay = true;

    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    public string Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string ConnectTimeoutMs
    {
        get => _connectTimeoutMs;
        set => SetProperty(ref _connectTimeoutMs, value);
    }

    public string BufferSize
    {
        get => _bufferSize;
        set => SetProperty(ref _bufferSize, value);
    }

    public bool NoDelay
    {
        get => _noDelay;
        set => SetProperty(ref _noDelay, value);
    }

    public override string Title => "TCP Target";

    public override string Subtitle => "Point-to-point socket session for controllers, PLCs, and simulators.";

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
