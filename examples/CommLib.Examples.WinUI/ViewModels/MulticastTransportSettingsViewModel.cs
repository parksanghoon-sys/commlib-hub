using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class MulticastTransportSettingsViewModel : TransportSettingsViewModel
{
    private string _groupAddress = "239.0.0.241";
    private string _port = "7004";
    private string _ttl = "1";
    private string _localInterface = string.Empty;
    private bool _loopback = true;

    public string GroupAddress
    {
        get => _groupAddress;
        set => SetProperty(ref _groupAddress, value);
    }

    public string Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string Ttl
    {
        get => _ttl;
        set => SetProperty(ref _ttl, value);
    }

    public string LocalInterface
    {
        get => _localInterface;
        set => SetProperty(ref _localInterface, value);
    }

    public bool Loopback
    {
        get => _loopback;
        set => SetProperty(ref _loopback, value);
    }

    public override string Title => "Multicast Target";

    public override string Subtitle => "Shared group traffic for one-to-many test rigs and listeners.";

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
