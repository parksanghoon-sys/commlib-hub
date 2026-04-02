using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class UdpTransportSettingsViewModel : TransportSettingsViewModel
{
    private string _localPort = "0";
    private string _remoteHost = "127.0.0.1";
    private string _remotePort = "7002";

    public string LocalPort
    {
        get => _localPort;
        set => SetProperty(ref _localPort, value);
    }

    public string RemoteHost
    {
        get => _remoteHost;
        set => SetProperty(ref _remoteHost, value);
    }

    public string RemotePort
    {
        get => _remotePort;
        set => SetProperty(ref _remotePort, value);
    }

    public override string Title => "UDP Target";

    public override string Subtitle => "Low-overhead datagrams for broadcast-style or loopback test flows.";

    public override TransportOptions BuildTransportOptions()
    {
        return new UdpTransportOptions
        {
            Type = "Udp",
            LocalPort = ParseInt(LocalPort, "UDP Local Port"),
            RemoteHost = NullIfWhiteSpace(RemoteHost),
            RemotePort = ParseOptionalInt(RemotePort, "UDP Remote Port")
        };
    }
}
