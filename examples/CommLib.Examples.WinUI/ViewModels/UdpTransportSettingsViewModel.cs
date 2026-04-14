using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// UdpTransportSettingsViewModel 타입입니다.
/// </summary>
public sealed class UdpTransportSettingsViewModel : TransportSettingsViewModel
{
    /// <summary>
    /// _localPort 값을 나타냅니다.
    /// </summary>
    private string _localPort = "0";
    /// <summary>
    /// _remoteHost 값을 나타냅니다.
    /// </summary>
    private string _remoteHost = "127.0.0.1";
    /// <summary>
    /// _remotePort 값을 나타냅니다.
    /// </summary>
    private string _remotePort = "7002";

    /// <summary>
    /// LocalPort 값을 가져옵니다.
    /// </summary>
    public string LocalPort
    {
        get => _localPort;
        set => SetProperty(ref _localPort, value);
    }

    /// <summary>
    /// RemoteHost 값을 가져옵니다.
    /// </summary>
    public string RemoteHost
    {
        get => _remoteHost;
        set => SetProperty(ref _remoteHost, value);
    }

    /// <summary>
    /// RemotePort 값을 가져옵니다.
    /// </summary>
    public string RemotePort
    {
        get => _remotePort;
        set => SetProperty(ref _remotePort, value);
    }

    /// <summary>
    /// Title 값을 가져옵니다.
    /// </summary>
    public override string Title => "UDP Target";

    /// <summary>
    /// Subtitle 값을 가져옵니다.
    /// </summary>
    public override string Subtitle => "Low-overhead datagrams for broadcast-style or loopback test flows.";

    /// <summary>
    /// BuildTransportOptions 작업을 수행합니다.
    /// </summary>
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