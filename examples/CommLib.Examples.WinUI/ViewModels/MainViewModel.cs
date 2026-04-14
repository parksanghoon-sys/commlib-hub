using System.Collections.ObjectModel;
using System.ComponentModel;
using CommLib.Application.Messaging;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// MainViewModel 타입입니다.
/// </summary>
public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    /// <summary>
    /// StatusOwner 값을 가져옵니다.
    /// </summary>
    private enum StatusOwner
    {
        // 화면이 직접 만든 임시 상태(예: Connecting, Sending)와
        // 서비스가 밀어 넣는 실시간 연결 상태를 구분해서 언어 변경 시 어느 쪽을 다시 렌더링할지 결정한다.
        ViewModel,
        Service
    }

    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;
    /// <summary>
    /// _mockEndpointService 값을 나타냅니다.
    /// </summary>
    private readonly ILocalMockEndpointService _mockEndpointService;
    /// <summary>
    /// _sessionService 값을 나타냅니다.
    /// </summary>
    private readonly IDeviceLabSessionService _sessionService;
    /// <summary>
    /// _uiDispatcher 값을 나타냅니다.
    /// </summary>
    private readonly IUiDispatcher _uiDispatcher;
    /// <summary>
    /// _statusText 값을 나타냅니다.
    /// </summary>
    private string _statusText = string.Empty;
    /// <summary>
    /// _statusDetail 값을 나타냅니다.
    /// </summary>
    private string _statusDetail = string.Empty;
    /// <summary>
    /// _statusTextKey 값을 나타냅니다.
    /// </summary>
    private string _statusTextKey = "main.status.disconnected";
    /// <summary>
    /// _statusDetailKey 값을 나타냅니다.
    /// </summary>
    private string _statusDetailKey = "main.detail.readyFirstSession";
    /// <summary>
    /// _statusTextArgs 값을 나타냅니다.
    /// </summary>
    private object[] _statusTextArgs = [];
    /// <summary>
    /// _statusDetailArgs 값을 나타냅니다.
    /// </summary>
    private object[] _statusDetailArgs = [];
    /// <summary>
    /// _statusOwner 값을 나타냅니다.
    /// </summary>
    private StatusOwner _statusOwner;
    /// <summary>
    /// _isBusy 값을 나타냅니다.
    /// </summary>
    private bool _isBusy;
    /// <summary>
    /// _isConnected 값을 나타냅니다.
    /// </summary>
    private bool _isConnected;
    /// <summary>
    /// _isMockEndpointBusy 값을 나타냅니다.
    /// </summary>
    private bool _isMockEndpointBusy;
    /// <summary>
    /// _isMockEndpointRunning 값을 나타냅니다.
    /// </summary>
    private bool _isMockEndpointRunning;
    /// <summary>
    /// _mockEndpointStatusTitle 값을 나타냅니다.
    /// </summary>
    private string _mockEndpointStatusTitle = string.Empty;
    /// <summary>
    /// _mockEndpointStatusDetail 값을 나타냅니다.
    /// </summary>
    private string _mockEndpointStatusDetail = string.Empty;
    /// <summary>
    /// _mockEndpointStatusTitleKey 값을 나타냅니다.
    /// </summary>
    private string _mockEndpointStatusTitleKey = "mockEndpoint.status.idle.title";
    /// <summary>
    /// _mockEndpointStatusDetailKey 값을 나타냅니다.
    /// </summary>
    private string _mockEndpointStatusDetailKey = "mockEndpoint.status.idle.detail";
    /// <summary>
    /// _mockEndpointStatusTitleArgs 값을 나타냅니다.
    /// </summary>
    private object[] _mockEndpointStatusTitleArgs = [];
    /// <summary>
    /// _mockEndpointStatusDetailArgs 값을 나타냅니다.
    /// </summary>
    private object[] _mockEndpointStatusDetailArgs = [];
    /// <summary>
    /// _usesRawMockEndpointDetail 값을 나타냅니다.
    /// </summary>
    private bool _usesRawMockEndpointDetail;
    /// <summary>
    /// _rawMockEndpointDetail 값을 나타냅니다.
    /// </summary>
    private string _rawMockEndpointDetail = string.Empty;
    /// <summary>
    /// _activeSerializerType 값을 나타냅니다.
    /// </summary>
    private string? _activeSerializerType;
    /// <summary>
    /// _activeMockEndpoint 값을 나타냅니다.
    /// </summary>
    private LocalMockEndpointBinding? _activeMockEndpoint;

    /// <summary>
    /// <see cref="MainViewModel"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public MainViewModel(
        ILocalMockEndpointService mockEndpointService,
        IDeviceLabSessionService sessionService,
        IUiDispatcher uiDispatcher,
        DeviceLabSettingsViewModel settings,
        IAppLocalizer localizer)
    {
        _localizer = localizer;
        _mockEndpointService = mockEndpointService;
        _sessionService = sessionService;
        _uiDispatcher = uiDispatcher;
        Settings = settings;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, CanDisconnect);
        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        ClearLogCommand = new RelayCommand(ClearLogs);
        StartMockEndpointCommand = new AsyncRelayCommand(StartMockEndpointAsync, CanStartMockEndpoint);
        StopMockEndpointCommand = new AsyncRelayCommand(StopMockEndpointAsync, CanStopMockEndpoint);

        _sessionService.LogEmitted += OnLogEmitted;
        _sessionService.ConnectionStateChanged += OnConnectionStateChanged;
        _localizer.LanguageChanged += OnLanguageChanged;
        Settings.PropertyChanged += OnSettingsPropertyChanged;

        // 첫 화면은 "연결 전 대기" 상태로 시작하지만,
        // mock peer 카드도 같이 보이므로 transport 선택에 맞는 보조 상태를 바로 계산해 둔다.
        ApplyViewStatus("main.status.disconnected", "main.detail.readyFirstSession");
        RefreshMockEndpointStatus();
        Logs.Add(new LogEntry(
            DateTimeOffset.Now,
            LogSeverity.Info,
            _localizer.Get("main.log.ready.title"),
            _localizer.Get("main.log.ready.message")));
    }

    /// <summary>
    /// Logs 값을 가져옵니다.
    /// </summary>
    public ObservableCollection<LogEntry> Logs { get; } = [];

    /// <summary>
    /// Settings 값을 가져옵니다.
    /// </summary>
    public DeviceLabSettingsViewModel Settings { get; }

    /// <summary>
    /// ConnectCommand 값을 가져옵니다.
    /// </summary>
    public AsyncRelayCommand ConnectCommand { get; }

    /// <summary>
    /// DisconnectCommand 값을 가져옵니다.
    /// </summary>
    public AsyncRelayCommand DisconnectCommand { get; }

    /// <summary>
    /// SendCommand 값을 가져옵니다.
    /// </summary>
    public AsyncRelayCommand SendCommand { get; }

    /// <summary>
    /// ClearLogCommand 값을 가져옵니다.
    /// </summary>
    public RelayCommand ClearLogCommand { get; }

    /// <summary>
    /// StartMockEndpointCommand 값을 가져옵니다.
    /// </summary>
    public AsyncRelayCommand StartMockEndpointCommand { get; }

    /// <summary>
    /// StopMockEndpointCommand 값을 가져옵니다.
    /// </summary>
    public AsyncRelayCommand StopMockEndpointCommand { get; }

    /// <summary>
    /// StatusText 값을 가져옵니다.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// StatusDetail 값을 가져옵니다.
    /// </summary>
    public string StatusDetail
    {
        get => _statusDetail;
        set => SetProperty(ref _statusDetail, value);
    }

    /// <summary>
    /// IsBusy 값을 가져옵니다.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    /// <summary>
    /// IsConnected 값을 가져옵니다.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    /// <summary>
    /// MockEndpointStatusTitle 값을 가져옵니다.
    /// </summary>
    public string MockEndpointStatusTitle
    {
        get => _mockEndpointStatusTitle;
        set => SetProperty(ref _mockEndpointStatusTitle, value);
    }

    /// <summary>
    /// MockEndpointStatusDetail 값을 가져옵니다.
    /// </summary>
    public string MockEndpointStatusDetail
    {
        get => _mockEndpointStatusDetail;
        set => SetProperty(ref _mockEndpointStatusDetail, value);
    }

    /// <summary>
    /// IsMockEndpointBusy 값을 가져옵니다.
    /// </summary>
    public bool IsMockEndpointBusy
    {
        get => _isMockEndpointBusy;
        set
        {
            if (SetProperty(ref _isMockEndpointBusy, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    /// <summary>
    /// IsMockEndpointRunning 값을 가져옵니다.
    /// </summary>
    public bool IsMockEndpointRunning
    {
        get => _isMockEndpointRunning;
        set
        {
            if (SetProperty(ref _isMockEndpointRunning, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    /// <summary>
    /// ProtocolBadgeText 값을 가져옵니다.
    /// </summary>
    public string ProtocolBadgeText => "LengthPrefixed";

    /// <summary>
    /// SerializerBadgeText 값을 가져옵니다.
    /// </summary>
    public string SerializerBadgeText => _activeSerializerType is null
        ? Settings.SelectedSerializerTitle
        : _localizer.GetSerializerLabel(_activeSerializerType);

    /// <summary>
    /// RuntimePolicyText 값을 가져옵니다.
    /// </summary>
    public string RuntimePolicyText => "Strict MVVM + DI + JSON";

    /// <summary>
    /// LogText 값을 가져옵니다.
    /// </summary>
    public string LogText => string.Join(Environment.NewLine, Logs.Select(log => log.ToString()));

    /// <summary>
    /// DisposeAsync 작업을 수행합니다.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Settings.PropertyChanged -= OnSettingsPropertyChanged;
        _localizer.LanguageChanged -= OnLanguageChanged;
        _sessionService.LogEmitted -= OnLogEmitted;
        _sessionService.ConnectionStateChanged -= OnConnectionStateChanged;
        await _mockEndpointService.DisposeAsync().ConfigureAwait(false);
        await _sessionService.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// CanConnect 작업을 수행합니다.
    /// </summary>
    private bool CanConnect()
    {
        return !IsBusy && !IsConnected;
    }

    /// <summary>
    /// CanDisconnect 작업을 수행합니다.
    /// </summary>
    private bool CanDisconnect()
    {
        return !IsBusy && IsConnected;
    }

    /// <summary>
    /// CanSend 작업을 수행합니다.
    /// </summary>
    private bool CanSend()
    {
        return !IsBusy && IsConnected;
    }

    /// <summary>
    /// CanStartMockEndpoint 작업을 수행합니다.
    /// </summary>
    private bool CanStartMockEndpoint()
    {
        return !IsBusy &&
               !IsConnected &&
               !IsMockEndpointBusy &&
               !IsMockEndpointRunning &&
               Settings.SelectedTransport.Kind != TransportKind.Serial;
    }

    /// <summary>
    /// CanStopMockEndpoint 작업을 수행합니다.
    /// </summary>
    private bool CanStopMockEndpoint()
    {
        return !IsMockEndpointBusy && IsMockEndpointRunning;
    }

    /// <summary>
    /// ConnectAsync 작업을 수행합니다.
    /// </summary>
    private async Task ConnectAsync()
    {
        IsBusy = true;
        ApplyViewStatus("main.status.connecting", "main.detail.negotiatingTransport");

        try
        {
            var profile = BuildProfile();
            await _sessionService.ConnectAsync(profile);
            _activeSerializerType = profile.Serializer.Type;
            OnPropertyChanged(nameof(SerializerBadgeText));
        }
        catch (Exception exception)
        {
            ApplyViewStatus("main.status.disconnected", "main.detail.connectionFailedBeforeOnline");
            AppendLog(new LogEntry(
                DateTimeOffset.Now,
                LogSeverity.Error,
                _localizer.Get("main.log.connectFailed.title"),
                exception.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// DisconnectAsync 작업을 수행합니다.
    /// </summary>
    private async Task DisconnectAsync()
    {
        IsBusy = true;
        ApplyViewStatus("main.status.disconnecting", "main.detail.shuttingDownTransport");

        try
        {
            await _sessionService.DisconnectAsync();
            _activeSerializerType = null;
            OnPropertyChanged(nameof(SerializerBadgeText));
        }
        catch (Exception exception)
        {
            AppendLog(new LogEntry(
                DateTimeOffset.Now,
                LogSeverity.Error,
                _localizer.Get("main.log.disconnectFailed.title"),
                exception.Message));
            ApplyViewStatus("main.status.disconnected", "main.detail.disconnectError");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// SendAsync 작업을 수행합니다.
    /// </summary>
    private async Task SendAsync()
    {
        IsBusy = true;
        ApplyViewStatus("main.status.sending", "main.detail.writingOutbound");

        try
        {
            await _sessionService.SendAsync(BuildOutboundMessage());
            ApplyViewStatus("main.status.connected", "main.detail.readyNextMessage");
        }
        catch (Exception exception)
        {
            AppendLog(new LogEntry(
                DateTimeOffset.Now,
                LogSeverity.Error,
                _localizer.Get("main.log.sendFailed.title"),
                exception.Message));
            ApplyViewStatus(
                IsConnected ? "main.status.connected" : "main.status.disconnected",
                IsConnected ? "main.detail.onlineAfterSendFailure" : "main.detail.sessionNotActive");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// StartMockEndpointAsync 작업을 수행합니다.
    /// </summary>
    private async Task StartMockEndpointAsync()
    {
        IsMockEndpointBusy = true;

        try
        {
            // mock peer는 현재 UI 설정을 그대로 읽되,
            // 실제 loopback 테스트에 필요한 최소 보정은 요청 생성 단계에서 함께 반영한다.
            var request = BuildMockEndpointRequest();
            var binding = await _mockEndpointService.StartAsync(request);
            _activeMockEndpoint = binding;
            IsMockEndpointRunning = true;
            ApplyRunningMockEndpointStatus(binding);
            AppendLog(new LogEntry(
                DateTimeOffset.Now,
                LogSeverity.Info,
                _localizer.Get("main.log.mockStarted.title"),
                BuildMockEndpointStartedMessage(binding)));
        }
        catch (Exception exception)
        {
            _activeMockEndpoint = null;
            IsMockEndpointRunning = false;
            SetMockEndpointStatusWithRawDetail("mockEndpoint.status.startFailed.title", exception.Message);
            AppendLog(new LogEntry(
                DateTimeOffset.Now,
                LogSeverity.Error,
                _localizer.Get("main.log.mockStartFailed.title"),
                exception.Message));
        }
        finally
        {
            IsMockEndpointBusy = false;
        }
    }

    /// <summary>
    /// StopMockEndpointAsync 작업을 수행합니다.
    /// </summary>
    private async Task StopMockEndpointAsync()
    {
        IsMockEndpointBusy = true;
        var previousMockEndpoint = _activeMockEndpoint;

        try
        {
            await _mockEndpointService.StopAsync();
            _activeMockEndpoint = null;
            IsMockEndpointRunning = false;
            RefreshMockEndpointStatus();

            if (previousMockEndpoint is not null)
            {
                AppendLog(new LogEntry(
                    DateTimeOffset.Now,
                    LogSeverity.Info,
                    _localizer.Get("main.log.mockStopped.title"),
                    _localizer.Format(
                        "main.log.mockStopped.message",
                        _localizer.GetTransportLabel(previousMockEndpoint.TransportKind))));
            }
        }
        catch (Exception exception)
        {
            SetMockEndpointStatusWithRawDetail("mockEndpoint.status.stopFailed.title", exception.Message);
            AppendLog(new LogEntry(
                DateTimeOffset.Now,
                LogSeverity.Error,
                _localizer.Get("main.log.mockStopFailed.title"),
                exception.Message));
        }
        finally
        {
            IsMockEndpointBusy = false;
        }
    }

    /// <summary>
    /// ClearLogs 작업을 수행합니다.
    /// </summary>
    private void ClearLogs()
    {
        Logs.Clear();
        Logs.Add(new LogEntry(
            DateTimeOffset.Now,
            LogSeverity.Info,
            _localizer.Get("main.log.consoleCleared.title"),
            _localizer.Get("main.log.consoleCleared.message")));
        OnPropertyChanged(nameof(LogText));
    }

    /// <summary>
    /// BuildProfile 작업을 수행합니다.
    /// </summary>
    private DeviceProfile BuildProfile()
    {
        var trimmedDeviceId = Settings.DeviceId.Trim();

        // Device Lab은 "현재 입력값을 바로 세션 프로필로 투영"하는 도구이므로
        // 별도 draft 모델을 두지 않고 실행 직전 한 번만 조합해서 넘긴다.
        return new DeviceProfile
        {
            DeviceId = trimmedDeviceId,
            DisplayName = string.IsNullOrWhiteSpace(Settings.DisplayName) ? trimmedDeviceId : Settings.DisplayName.Trim(),
            Enabled = true,
            Transport = BuildTransportOptions(),
            Protocol = new ProtocolOptions
            {
                Type = "LengthPrefixed",
                MaxFrameLength = 4096
            },
            Serializer = new SerializerOptions
            {
                Type = Settings.SelectedSerializer.Type
            },
            Reconnect = new ReconnectOptions
            {
                Type = "None",
                MaxAttempts = 0
            },
            RequestResponse = new RequestResponseOptions
            {
                DefaultTimeoutMs = ParsePositiveInt(Settings.DefaultTimeoutMs, "Default Timeout"),
                MaxPendingRequests = ParsePositiveInt(Settings.MaxPendingRequests, "Max Pending Requests")
            }
        };
    }

    /// <summary>
    /// BuildMockEndpointRequest 작업을 수행합니다.
    /// </summary>
    private LocalMockEndpointRequest BuildMockEndpointRequest()
    {
        // transport별로 mock peer가 기대하는 최소 입력 형식이 조금씩 다르다.
        // 이 메서드는 ViewModel의 현재 UI 문자열 설정을 "실행 가능한 mock 요청"으로 바꾸는 경계다.
        return Settings.SelectedTransport.Kind switch
        {
            TransportKind.Tcp => BuildTcpMockEndpointRequest(),
            TransportKind.Udp => BuildUdpMockEndpointRequest(),
            TransportKind.Multicast => BuildMulticastMockEndpointRequest(),
            TransportKind.Serial => throw new NotSupportedException(
                _localizer.Get("mockEndpoint.status.unavailable.serialDetail")),
            _ => throw new InvalidOperationException("Unsupported transport selection.")
        };
    }

    /// <summary>
    /// BuildTransportOptions 작업을 수행합니다.
    /// </summary>
    private TransportOptions BuildTransportOptions()
    {
        return Settings.SelectedTransport.Kind switch
        {
            TransportKind.Tcp => Settings.TcpSettings.BuildTransportOptions(),
            TransportKind.Udp => Settings.UdpSettings.BuildTransportOptions(),
            TransportKind.Multicast => Settings.MulticastSettings.BuildTransportOptions(),
            TransportKind.Serial => Settings.SerialSettings.BuildTransportOptions(),
            _ => throw new InvalidOperationException("Unsupported transport selection.")
        };
    }

    /// <summary>
    /// ParseMessageId 작업을 수행합니다.
    /// </summary>
    private ushort ParseMessageId()
    {
        if (!ushort.TryParse(Settings.OutboundMessageId.Trim(), out var messageId))
        {
            throw new InvalidOperationException("Message Id must be between 0 and 65535.");
        }

        return messageId;
    }

    /// <summary>
    /// BuildOutboundMessage 작업을 수행합니다.
    /// </summary>
    private IMessage BuildOutboundMessage()
    {
        var serializerType = _activeSerializerType ?? Settings.SelectedSerializer.Type;

        try
        {
            return OutboundMessageComposer.Compose(serializerType, ParseMessageId(), Settings.OutboundBody);
        }
        catch (FormatException) when (string.Equals(serializerType, SerializerTypes.RawHex, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(_localizer.Get("composer.error.invalidRawHex"));
        }
    }

    /// <summary>
    /// NotifyCommandStateChanged 작업을 수행합니다.
    /// </summary>
    private void NotifyCommandStateChanged()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SendCommand.NotifyCanExecuteChanged();
        StartMockEndpointCommand.NotifyCanExecuteChanged();
        StopMockEndpointCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// ParsePositiveInt 작업을 수행합니다.
    /// </summary>
    private static int ParsePositiveInt(string value, string fieldName)
    {
        if (!int.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"{fieldName} must be an integer.");
        }

        if (parsed <= 0)
        {
            throw new InvalidOperationException($"{fieldName} must be greater than zero.");
        }

        return parsed;
    }

    /// <summary>
    /// OnLogEmitted 작업을 수행합니다.
    /// </summary>
    private void OnLogEmitted(object? sender, LogEntry entry)
    {
        AppendLog(entry);
    }

    /// <summary>
    /// OnConnectionStateChanged 작업을 수행합니다.
    /// </summary>
    private void OnConnectionStateChanged(object? sender, ConnectionStateSnapshot snapshot)
    {
        // 세션 서비스 콜백은 background thread에서 들어올 수 있으므로
        // ObservableCollection/바인딩 상태 변경은 항상 UI dispatcher를 거쳐 수행한다.
        _uiDispatcher.Enqueue(() =>
        {
            _statusOwner = StatusOwner.Service;
            IsConnected = snapshot.IsConnected;
            if (!snapshot.IsConnected)
            {
                _activeSerializerType = null;
                OnPropertyChanged(nameof(SerializerBadgeText));
            }

            StatusText = snapshot.StatusText;
            StatusDetail = snapshot.StatusDetail;
        });
    }

    /// <summary>
    /// AppendLog 작업을 수행합니다.
    /// </summary>
    private void AppendLog(LogEntry entry)
    {
        _uiDispatcher.Enqueue(() =>
        {
            Logs.Add(entry);
            // TextBox는 컬렉션이 아니라 단일 문자열 바인딩을 보고 있으므로
            // 로그가 추가될 때마다 LogText 변경 알림을 직접 일으켜야 한다.
            OnPropertyChanged(nameof(LogText));
        });
    }

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        if (_statusOwner == StatusOwner.ViewModel)
        {
            RefreshViewStatus();
        }

        RefreshMockEndpointStatus();
    }

    /// <summary>
    /// OnSettingsPropertyChanged 작업을 수행합니다.
    /// </summary>
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(DeviceLabSettingsViewModel.SelectedSerializer))
        {
            if (_activeSerializerType is null)
            {
                OnPropertyChanged(nameof(SerializerBadgeText));
            }
        }

        if (args.PropertyName is nameof(DeviceLabSettingsViewModel.SelectedTransport))
        {
            if (!IsMockEndpointRunning)
            {
                RefreshMockEndpointStatus();
            }

            NotifyCommandStateChanged();
        }
    }

    /// <summary>
    /// ApplyViewStatus 작업을 수행합니다.
    /// </summary>
    private void ApplyViewStatus(
        string statusTextKey,
        string statusDetailKey,
        object[]? statusTextArgs = null,
        object[]? statusDetailArgs = null)
    {
        _statusOwner = StatusOwner.ViewModel;
        _statusTextKey = statusTextKey;
        _statusDetailKey = statusDetailKey;
        _statusTextArgs = statusTextArgs ?? [];
        _statusDetailArgs = statusDetailArgs ?? [];
        RefreshViewStatus();
    }

    /// <summary>
    /// RefreshViewStatus 작업을 수행합니다.
    /// </summary>
    private void RefreshViewStatus()
    {
        StatusText = _localizer.Format(_statusTextKey, _statusTextArgs);
        StatusDetail = _localizer.Format(_statusDetailKey, _statusDetailArgs);
    }

    /// <summary>
    /// BuildTcpMockEndpointRequest 작업을 수행합니다.
    /// </summary>
    private LocalMockEndpointRequest BuildTcpMockEndpointRequest()
    {
        Settings.TcpSettings.Host = "127.0.0.1";
        return new LocalMockEndpointRequest(
            TransportKind.Tcp,
            ParsePositiveInt(Settings.TcpSettings.Port, "TCP Port"));
    }

    /// <summary>
    /// BuildUdpMockEndpointRequest 작업을 수행합니다.
    /// </summary>
    private LocalMockEndpointRequest BuildUdpMockEndpointRequest()
    {
        Settings.UdpSettings.RemoteHost = "127.0.0.1";
        var remotePort = ParsePositiveInt(Settings.UdpSettings.RemotePort, "UDP Remote Port");

        // 로컬 포트와 원격 포트를 같은 값으로 고정하면 한 프로세스에서 충돌하기 쉬워서
        // 사용자가 같은 값을 넣어 둔 경우 mock 모드에서는 ephemeral 포트로 풀어 준다.
        if (int.TryParse(Settings.UdpSettings.LocalPort.Trim(), out var localPort) && localPort == remotePort)
        {
            Settings.UdpSettings.LocalPort = "0";
        }

        return new LocalMockEndpointRequest(TransportKind.Udp, remotePort);
    }

    /// <summary>
    /// BuildMulticastMockEndpointRequest 작업을 수행합니다.
    /// </summary>
    private LocalMockEndpointRequest BuildMulticastMockEndpointRequest()
    {
        Settings.MulticastSettings.Loopback = true;
        return new LocalMockEndpointRequest(
            TransportKind.Multicast,
            ParsePositiveInt(Settings.MulticastSettings.Port, "Multicast Port"),
            Settings.MulticastSettings.GroupAddress.Trim(),
            string.IsNullOrWhiteSpace(Settings.MulticastSettings.LocalInterface)
                ? null
                : Settings.MulticastSettings.LocalInterface.Trim());
    }

    /// <summary>
    /// RefreshMockEndpointStatus 작업을 수행합니다.
    /// </summary>
    private void RefreshMockEndpointStatus()
    {
        if (_activeMockEndpoint is not null && IsMockEndpointRunning)
        {
            ApplyRunningMockEndpointStatus(_activeMockEndpoint);
            return;
        }

        // Serial은 앱 내부에서 진짜 peer를 흉내 낼 수 없으므로
        // 버튼 비활성화와 함께 이유를 상태 문구에서도 분명히 보여 준다.
        if (Settings.SelectedTransport.Kind == TransportKind.Serial)
        {
            SetLocalizedMockEndpointStatus(
                "mockEndpoint.status.unavailable.title",
                "mockEndpoint.status.unavailable.serialDetail");
            return;
        }

        SetLocalizedMockEndpointStatus(
            "mockEndpoint.status.idle.title",
            "mockEndpoint.status.idle.detail",
            detailArgs: [_localizer.GetTransportLabel(Settings.SelectedTransport.Kind)]);
    }

    /// <summary>
    /// ApplyRunningMockEndpointStatus 작업을 수행합니다.
    /// </summary>
    private void ApplyRunningMockEndpointStatus(LocalMockEndpointBinding binding)
    {
        var detailKey = binding.TransportKind switch
        {
            TransportKind.Tcp => "mockEndpoint.status.running.tcpDetail",
            TransportKind.Udp => "mockEndpoint.status.running.udpDetail",
            TransportKind.Multicast => "mockEndpoint.status.running.multicastDetail",
            _ => "mockEndpoint.status.idle.detail"
        };

        SetLocalizedMockEndpointStatus(
            "mockEndpoint.status.running.title",
            detailKey,
            titleArgs: [_localizer.GetTransportLabel(binding.TransportKind)],
            detailArgs: [binding.Address, binding.Port]);
    }

    /// <summary>
    /// SetLocalizedMockEndpointStatus 작업을 수행합니다.
    /// </summary>
    private void SetLocalizedMockEndpointStatus(
        string titleKey,
        string detailKey,
        object[]? titleArgs = null,
        object[]? detailArgs = null)
    {
        _mockEndpointStatusTitleKey = titleKey;
        _mockEndpointStatusDetailKey = detailKey;
        _mockEndpointStatusTitleArgs = titleArgs ?? [];
        _mockEndpointStatusDetailArgs = detailArgs ?? [];
        _usesRawMockEndpointDetail = false;
        _rawMockEndpointDetail = string.Empty;
        ApplyMockEndpointStatus();
    }

    /// <summary>
    /// SetMockEndpointStatusWithRawDetail 작업을 수행합니다.
    /// </summary>
    private void SetMockEndpointStatusWithRawDetail(string titleKey, string rawDetail, object[]? titleArgs = null)
    {
        _mockEndpointStatusTitleKey = titleKey;
        _mockEndpointStatusTitleArgs = titleArgs ?? [];
        _usesRawMockEndpointDetail = true;
        _rawMockEndpointDetail = rawDetail;
        ApplyMockEndpointStatus();
    }

    /// <summary>
    /// ApplyMockEndpointStatus 작업을 수행합니다.
    /// </summary>
    private void ApplyMockEndpointStatus()
    {
        MockEndpointStatusTitle = _localizer.Format(_mockEndpointStatusTitleKey, _mockEndpointStatusTitleArgs);
        MockEndpointStatusDetail = _usesRawMockEndpointDetail
            ? _rawMockEndpointDetail
            : _localizer.Format(_mockEndpointStatusDetailKey, _mockEndpointStatusDetailArgs);
    }

    /// <summary>
    /// BuildMockEndpointStartedMessage 작업을 수행합니다.
    /// </summary>
    private string BuildMockEndpointStartedMessage(LocalMockEndpointBinding binding)
    {
        var detailKey = binding.TransportKind switch
        {
            TransportKind.Tcp => "mockEndpoint.status.running.tcpDetail",
            TransportKind.Udp => "mockEndpoint.status.running.udpDetail",
            TransportKind.Multicast => "mockEndpoint.status.running.multicastDetail",
            _ => "mockEndpoint.status.idle.detail"
        };

        return _localizer.Format(detailKey, binding.Address, binding.Port);
    }
}