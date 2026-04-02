using CommLib.Examples.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class DeviceLabSettingsViewModel : ObservableObject
{
    private string _deviceId = "device-lab";
    private string _displayName = "Device Lab";
    private string _defaultTimeoutMs = "3000";
    private string _maxPendingRequests = "8";
    private string _outboundMessageId = "100";
    private string _outboundBody = "hello from the mvvm winui lab";
    private TransportChoiceViewModel _selectedTransport = null!;

    public DeviceLabSettingsViewModel(
        TcpTransportSettingsViewModel tcpSettings,
        UdpTransportSettingsViewModel udpSettings,
        MulticastTransportSettingsViewModel multicastSettings,
        SerialTransportSettingsViewModel serialSettings)
    {
        TcpSettings = tcpSettings;
        UdpSettings = udpSettings;
        MulticastSettings = multicastSettings;
        SerialSettings = serialSettings;

        TransportChoices =
        [
            new TransportChoiceViewModel(TransportKind.Tcp, "TCP", "Reliable socket session"),
            new TransportChoiceViewModel(TransportKind.Udp, "UDP", "Fast datagram transport"),
            new TransportChoiceViewModel(TransportKind.Multicast, "Multicast", "Group broadcast traffic"),
            new TransportChoiceViewModel(TransportKind.Serial, "Serial", "COM and loopback links")
        ];

        _selectedTransport = TransportChoices[0];
    }

    public IReadOnlyList<TransportChoiceViewModel> TransportChoices { get; }

    public TcpTransportSettingsViewModel TcpSettings { get; }

    public UdpTransportSettingsViewModel UdpSettings { get; }

    public MulticastTransportSettingsViewModel MulticastSettings { get; }

    public SerialTransportSettingsViewModel SerialSettings { get; }

    public string DeviceId
    {
        get => _deviceId;
        set => SetProperty(ref _deviceId, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string DefaultTimeoutMs
    {
        get => _defaultTimeoutMs;
        set => SetProperty(ref _defaultTimeoutMs, value);
    }

    public string MaxPendingRequests
    {
        get => _maxPendingRequests;
        set => SetProperty(ref _maxPendingRequests, value);
    }

    public string OutboundMessageId
    {
        get => _outboundMessageId;
        set => SetProperty(ref _outboundMessageId, value);
    }

    public string OutboundBody
    {
        get => _outboundBody;
        set => SetProperty(ref _outboundBody, value);
    }

    public TransportChoiceViewModel SelectedTransport
    {
        get => _selectedTransport;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (SetProperty(ref _selectedTransport, value))
            {
                OnPropertyChanged(nameof(IsTcpSelected));
                OnPropertyChanged(nameof(IsUdpSelected));
                OnPropertyChanged(nameof(IsMulticastSelected));
                OnPropertyChanged(nameof(IsSerialSelected));
                OnPropertyChanged(nameof(SelectedTransportTitle));
                OnPropertyChanged(nameof(SelectedTransportSubtitle));
            }
        }
    }

    public bool IsTcpSelected => SelectedTransport.Kind == TransportKind.Tcp;

    public bool IsUdpSelected => SelectedTransport.Kind == TransportKind.Udp;

    public bool IsMulticastSelected => SelectedTransport.Kind == TransportKind.Multicast;

    public bool IsSerialSelected => SelectedTransport.Kind == TransportKind.Serial;

    public string SelectedTransportTitle => SelectedTransport.Label;

    public string SelectedTransportSubtitle => SelectedTransport.Subtitle;

    public DeviceLabAppSettings CreateSnapshot()
    {
        return new DeviceLabAppSettings
        {
            Session = new SessionAppSettings
            {
                DeviceId = DeviceId,
                DisplayName = DisplayName,
                DefaultTimeoutMs = DefaultTimeoutMs,
                MaxPendingRequests = MaxPendingRequests,
                SelectedTransport = SelectedTransport.Kind
            },
            MessageComposer = new MessageComposerAppSettings
            {
                OutboundMessageId = OutboundMessageId,
                OutboundBody = OutboundBody
            },
            Tcp = new TcpTransportAppSettings
            {
                Host = TcpSettings.Host,
                Port = TcpSettings.Port,
                ConnectTimeoutMs = TcpSettings.ConnectTimeoutMs,
                BufferSize = TcpSettings.BufferSize,
                NoDelay = TcpSettings.NoDelay
            },
            Udp = new UdpTransportAppSettings
            {
                LocalPort = UdpSettings.LocalPort,
                RemoteHost = UdpSettings.RemoteHost,
                RemotePort = UdpSettings.RemotePort
            },
            Multicast = new MulticastTransportAppSettings
            {
                GroupAddress = MulticastSettings.GroupAddress,
                Port = MulticastSettings.Port,
                Ttl = MulticastSettings.Ttl,
                LocalInterface = MulticastSettings.LocalInterface,
                Loopback = MulticastSettings.Loopback
            },
            Serial = new SerialTransportAppSettings
            {
                PortName = SerialSettings.PortName,
                BaudRate = SerialSettings.BaudRate,
                DataBits = SerialSettings.DataBits,
                Parity = SerialSettings.Parity,
                StopBits = SerialSettings.StopBits,
                TurnGapMs = SerialSettings.TurnGapMs,
                ReadBufferSize = SerialSettings.ReadBufferSize,
                WriteBufferSize = SerialSettings.WriteBufferSize,
                HalfDuplex = SerialSettings.HalfDuplex
            }
        };
    }

    public void Apply(DeviceLabAppSettings? settings)
    {
        settings ??= new DeviceLabAppSettings();

        DeviceId = settings.Session.DeviceId;
        DisplayName = settings.Session.DisplayName;
        DefaultTimeoutMs = settings.Session.DefaultTimeoutMs;
        MaxPendingRequests = settings.Session.MaxPendingRequests;
        OutboundMessageId = settings.MessageComposer.OutboundMessageId;
        OutboundBody = settings.MessageComposer.OutboundBody;

        TcpSettings.Host = settings.Tcp.Host;
        TcpSettings.Port = settings.Tcp.Port;
        TcpSettings.ConnectTimeoutMs = settings.Tcp.ConnectTimeoutMs;
        TcpSettings.BufferSize = settings.Tcp.BufferSize;
        TcpSettings.NoDelay = settings.Tcp.NoDelay;

        UdpSettings.LocalPort = settings.Udp.LocalPort;
        UdpSettings.RemoteHost = settings.Udp.RemoteHost;
        UdpSettings.RemotePort = settings.Udp.RemotePort;

        MulticastSettings.GroupAddress = settings.Multicast.GroupAddress;
        MulticastSettings.Port = settings.Multicast.Port;
        MulticastSettings.Ttl = settings.Multicast.Ttl;
        MulticastSettings.LocalInterface = settings.Multicast.LocalInterface;
        MulticastSettings.Loopback = settings.Multicast.Loopback;

        SerialSettings.PortName = settings.Serial.PortName;
        SerialSettings.BaudRate = settings.Serial.BaudRate;
        SerialSettings.DataBits = settings.Serial.DataBits;
        SerialSettings.Parity = settings.Serial.Parity;
        SerialSettings.StopBits = settings.Serial.StopBits;
        SerialSettings.TurnGapMs = settings.Serial.TurnGapMs;
        SerialSettings.ReadBufferSize = settings.Serial.ReadBufferSize;
        SerialSettings.WriteBufferSize = settings.Serial.WriteBufferSize;
        SerialSettings.HalfDuplex = settings.Serial.HalfDuplex;

        SelectedTransport = ResolveTransportChoice(settings.Session.SelectedTransport);
    }

    public void ResetToDefaults()
    {
        Apply(new DeviceLabAppSettings());
    }

    private TransportChoiceViewModel ResolveTransportChoice(TransportKind kind)
    {
        return TransportChoices.FirstOrDefault(choice => choice.Kind == kind) ?? TransportChoices[0];
    }
}
