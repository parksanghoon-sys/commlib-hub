using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// MulticastTransportSettingsViewModel 타입입니다.
/// </summary>
public sealed class MulticastTransportSettingsViewModel : TransportSettingsViewModel
{
    /// <summary>
    /// _groupAddress 값을 나타냅니다.
    /// </summary>
    private string _groupAddress = "239.0.0.241";
    /// <summary>
    /// _port 값을 나타냅니다.
    /// </summary>
    private string _port = "7004";
    /// <summary>
    /// _ttl 값을 나타냅니다.
    /// </summary>
    private string _ttl = "1";
    /// <summary>
    /// _localInterface 값을 나타냅니다.
    /// </summary>
    private string _localInterface = string.Empty;
    /// <summary>
    /// _loopback 값을 나타냅니다.
    /// </summary>
    private bool _loopback = true;

    /// <summary>
    /// GroupAddress 값을 가져옵니다.
    /// </summary>
    public string GroupAddress
    {
        get => _groupAddress;
        set => SetProperty(ref _groupAddress, value);
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
    /// Ttl 값을 가져옵니다.
    /// </summary>
    public string Ttl
    {
        get => _ttl;
        set => SetProperty(ref _ttl, value);
    }

    /// <summary>
    /// LocalInterface 값을 가져옵니다.
    /// </summary>
    public string LocalInterface
    {
        get => _localInterface;
        set => SetProperty(ref _localInterface, value);
    }

    /// <summary>
    /// Loopback 값을 가져옵니다.
    /// </summary>
    public bool Loopback
    {
        get => _loopback;
        set => SetProperty(ref _loopback, value);
    }

    /// <summary>
    /// Title 값을 가져옵니다.
    /// </summary>
    public override string Title => "Multicast Target";

    /// <summary>
    /// Subtitle 값을 가져옵니다.
    /// </summary>
    public override string Subtitle => "Shared group traffic for one-to-many test rigs and listeners.";

    /// <summary>
    /// BuildTransportOptions 작업을 수행합니다.
    /// </summary>
    public override TransportOptions BuildTransportOptions()
    {
        return new MulticastTransportOptions
        {
            Type = "Multicast",
            GroupAddress = GroupAddress.Trim(),
            Port = ParseInt(Port, "Multicast Port"),
            Ttl = ParseInt(Ttl, "Multicast TTL"),
            LocalInterface = NullIfWhiteSpace(LocalInterface),
            Loopback = Loopback
        };
    }
}