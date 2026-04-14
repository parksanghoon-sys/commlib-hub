using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// DeviceLabSettingsViewModel 타입입니다.
/// </summary>
public sealed class DeviceLabSettingsViewModel : ObservableObject
{
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;
    /// <summary>
    /// _deviceId 값을 나타냅니다.
    /// </summary>
    private string _deviceId = "device-lab";
    /// <summary>
    /// _displayName 값을 나타냅니다.
    /// </summary>
    private string _displayName = "Device Lab";
    /// <summary>
    /// _defaultTimeoutMs 값을 나타냅니다.
    /// </summary>
    private string _defaultTimeoutMs = "3000";
    /// <summary>
    /// _maxPendingRequests 값을 나타냅니다.
    /// </summary>
    private string _maxPendingRequests = "8";
    /// <summary>
    /// _outboundMessageId 값을 나타냅니다.
    /// </summary>
    private string _outboundMessageId = "100";
    /// <summary>
    /// _outboundBody 값을 나타냅니다.
    /// </summary>
    private string _outboundBody = "hello from the mvvm winui lab";
    /// <summary>
    /// _bitFieldSchema 값을 나타냅니다.
    /// </summary>
    private BitFieldPayloadSchema? _bitFieldSchema;
    /// <summary>
    /// _selectedLanguage 값을 나타냅니다.
    /// </summary>
    private LanguageChoiceViewModel _selectedLanguage = null!;
    /// <summary>
    /// _selectedSerializer 값을 나타냅니다.
    /// </summary>
    private SerializerChoiceViewModel _selectedSerializer = null!;
    /// <summary>
    /// _selectedTransport 값을 나타냅니다.
    /// </summary>
    private TransportChoiceViewModel _selectedTransport = null!;

    /// <summary>
    /// <see cref="DeviceLabSettingsViewModel"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public DeviceLabSettingsViewModel(
        IAppLocalizer localizer,
        TcpTransportSettingsViewModel tcpSettings,
        UdpTransportSettingsViewModel udpSettings,
        MulticastTransportSettingsViewModel multicastSettings,
        SerialTransportSettingsViewModel serialSettings)
    {
        _localizer = localizer;
        TcpSettings = tcpSettings;
        UdpSettings = udpSettings;
        MulticastSettings = multicastSettings;
        SerialSettings = serialSettings;

        LanguageChoices =
        [
            new LanguageChoiceViewModel(AppLanguageMode.English, _localizer),
            new LanguageChoiceViewModel(AppLanguageMode.Korean, _localizer)
        ];

        SerializerChoices =
        [
            new SerializerChoiceViewModel(SerializerTypes.AutoBinary, _localizer),
            new SerializerChoiceViewModel(SerializerTypes.RawHex, _localizer)
        ];

        TransportChoices =
        [
            new TransportChoiceViewModel(TransportKind.Tcp, _localizer),
            new TransportChoiceViewModel(TransportKind.Udp, _localizer),
            new TransportChoiceViewModel(TransportKind.Multicast, _localizer),
            new TransportChoiceViewModel(TransportKind.Serial, _localizer)
        ];

        _selectedLanguage = LanguageChoices[0];
        _selectedSerializer = SerializerChoices[0];
        _selectedTransport = TransportChoices[0];
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// LanguageChoices 값을 가져옵니다.
    /// </summary>
    public IReadOnlyList<LanguageChoiceViewModel> LanguageChoices { get; }

    /// <summary>
    /// SerializerChoices 값을 가져옵니다.
    /// </summary>
    public IReadOnlyList<SerializerChoiceViewModel> SerializerChoices { get; }

    /// <summary>
    /// TransportChoices 값을 가져옵니다.
    /// </summary>
    public IReadOnlyList<TransportChoiceViewModel> TransportChoices { get; }

    /// <summary>
    /// TcpSettings 값을 가져옵니다.
    /// </summary>
    public TcpTransportSettingsViewModel TcpSettings { get; }

    /// <summary>
    /// UdpSettings 값을 가져옵니다.
    /// </summary>
    public UdpTransportSettingsViewModel UdpSettings { get; }

    /// <summary>
    /// MulticastSettings 값을 가져옵니다.
    /// </summary>
    public MulticastTransportSettingsViewModel MulticastSettings { get; }

    /// <summary>
    /// SerialSettings 값을 가져옵니다.
    /// </summary>
    public SerialTransportSettingsViewModel SerialSettings { get; }

    /// <summary>
    /// DeviceId 값을 가져옵니다.
    /// </summary>
    public string DeviceId
    {
        get => _deviceId;
        set => SetProperty(ref _deviceId, value);
    }

    /// <summary>
    /// DisplayName 값을 가져옵니다.
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    /// <summary>
    /// DefaultTimeoutMs 값을 가져옵니다.
    /// </summary>
    public string DefaultTimeoutMs
    {
        get => _defaultTimeoutMs;
        set => SetProperty(ref _defaultTimeoutMs, value);
    }

    /// <summary>
    /// MaxPendingRequests 값을 가져옵니다.
    /// </summary>
    public string MaxPendingRequests
    {
        get => _maxPendingRequests;
        set => SetProperty(ref _maxPendingRequests, value);
    }

    /// <summary>
    /// OutboundMessageId 값을 가져옵니다.
    /// </summary>
    public string OutboundMessageId
    {
        get => _outboundMessageId;
        set => SetProperty(ref _outboundMessageId, value);
    }

    /// <summary>
    /// OutboundBody 값을 가져옵니다.
    /// </summary>
    public string OutboundBody
    {
        get => _outboundBody;
        set => SetProperty(ref _outboundBody, value);
    }

    /// <summary>
    /// BitFieldSchema 값을 가져옵니다.
    /// </summary>
    public BitFieldPayloadSchema? BitFieldSchema
    {
        get => _bitFieldSchema;
        set => SetProperty(ref _bitFieldSchema, value);
    }

    /// <summary>
    /// SelectedLanguage 값을 가져옵니다.
    /// </summary>
    public LanguageChoiceViewModel SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (SetProperty(ref _selectedLanguage, value))
            {
                _localizer.CurrentLanguage = value.Mode;
            }
        }
    }

    /// <summary>
    /// SelectedSerializer 값을 가져옵니다.
    /// </summary>
    public SerializerChoiceViewModel SelectedSerializer
    {
        get => _selectedSerializer;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (SetProperty(ref _selectedSerializer, value))
            {
                OnPropertyChanged(nameof(SelectedSerializerTitle));
                OnPropertyChanged(nameof(SelectedSerializerSubtitle));
            }
        }
    }

    /// <summary>
    /// SelectedTransport 값을 가져옵니다.
    /// </summary>
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

    /// <summary>
    /// IsTcpSelected 값을 가져옵니다.
    /// </summary>
    public bool IsTcpSelected => SelectedTransport.Kind == TransportKind.Tcp;

    /// <summary>
    /// IsUdpSelected 값을 가져옵니다.
    /// </summary>
    public bool IsUdpSelected => SelectedTransport.Kind == TransportKind.Udp;

    /// <summary>
    /// IsMulticastSelected 값을 가져옵니다.
    /// </summary>
    public bool IsMulticastSelected => SelectedTransport.Kind == TransportKind.Multicast;

    /// <summary>
    /// IsSerialSelected 값을 가져옵니다.
    /// </summary>
    public bool IsSerialSelected => SelectedTransport.Kind == TransportKind.Serial;

    /// <summary>
    /// SelectedTransportTitle 값을 가져옵니다.
    /// </summary>
    public string SelectedTransportTitle => SelectedTransport.Label;

    /// <summary>
    /// SelectedTransportSubtitle 값을 가져옵니다.
    /// </summary>
    public string SelectedTransportSubtitle => SelectedTransport.Subtitle;

    /// <summary>
    /// SelectedSerializerTitle 값을 가져옵니다.
    /// </summary>
    public string SelectedSerializerTitle => SelectedSerializer.Label;

    /// <summary>
    /// SelectedSerializerSubtitle 값을 가져옵니다.
    /// </summary>
    public string SelectedSerializerSubtitle => SelectedSerializer.Subtitle;

    /// <summary>
    /// CreateSnapshot 작업을 수행합니다.
    /// </summary>
    public DeviceLabAppSettings CreateSnapshot()
    {
        return new DeviceLabAppSettings
        {
            Ui = new UiAppSettings
            {
                LanguageMode = SelectedLanguage.Mode
            },
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
                SerializerType = SelectedSerializer.Type,
                BitFieldSchema = BitFieldSchema,
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

    /// <summary>
    /// Apply 작업을 수행합니다.
    /// </summary>
    public void Apply(DeviceLabAppSettings? settings)
    {
        settings ??= new DeviceLabAppSettings();

        SelectedLanguage = ResolveLanguageChoice(settings.Ui.LanguageMode);
        DeviceId = settings.Session.DeviceId;
        DisplayName = settings.Session.DisplayName;
        DefaultTimeoutMs = settings.Session.DefaultTimeoutMs;
        MaxPendingRequests = settings.Session.MaxPendingRequests;
        SelectedSerializer = ResolveSerializerChoice(settings.MessageComposer.SerializerType);
        BitFieldSchema = settings.MessageComposer.BitFieldSchema;
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

    /// <summary>
    /// ResetToDefaults 작업을 수행합니다.
    /// </summary>
    public void ResetToDefaults()
    {
        Apply(new DeviceLabAppSettings());
    }

    /// <summary>
    /// ResolveTransportChoice 작업을 수행합니다.
    /// </summary>
    private TransportChoiceViewModel ResolveTransportChoice(TransportKind kind)
    {
        return TransportChoices.FirstOrDefault(choice => choice.Kind == kind) ?? TransportChoices[0];
    }

    /// <summary>
    /// ResolveSerializerChoice 작업을 수행합니다.
    /// </summary>
    private SerializerChoiceViewModel ResolveSerializerChoice(string type)
    {
        return SerializerChoices.FirstOrDefault(choice => choice.Type == type) ?? SerializerChoices[0];
    }

    /// <summary>
    /// ResolveLanguageChoice 작업을 수행합니다.
    /// </summary>
    private LanguageChoiceViewModel ResolveLanguageChoice(AppLanguageMode mode)
    {
        return LanguageChoices.FirstOrDefault(choice => choice.Mode == mode) ?? LanguageChoices[0];
    }

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(SelectedSerializerTitle));
        OnPropertyChanged(nameof(SelectedSerializerSubtitle));
        OnPropertyChanged(nameof(SelectedTransportTitle));
        OnPropertyChanged(nameof(SelectedTransportSubtitle));
    }
}