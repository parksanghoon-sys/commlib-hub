using System.Collections.ObjectModel;
using CommLib.Domain.Configuration;
using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IDeviceLabSessionService _sessionService;
    private readonly IUiDispatcher _uiDispatcher;
    private string _statusText = "Disconnected";
    private string _statusDetail = "Ready for the first device session.";
    private bool _isBusy;
    private bool _isConnected;

    public MainViewModel(
        IDeviceLabSessionService sessionService,
        IUiDispatcher uiDispatcher,
        DeviceLabSettingsViewModel settings)
    {
        _sessionService = sessionService;
        _uiDispatcher = uiDispatcher;
        Settings = settings;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, CanDisconnect);
        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        ClearLogCommand = new RelayCommand(ClearLogs);

        _sessionService.LogEmitted += OnLogEmitted;
        _sessionService.ConnectionStateChanged += OnConnectionStateChanged;

        Logs.Add(new LogEntry(DateTimeOffset.Now, LogSeverity.Info, "Ready", "Choose a transport and open a session."));
    }

    public ObservableCollection<LogEntry> Logs { get; } = [];

    public DeviceLabSettingsViewModel Settings { get; }

    public AsyncRelayCommand ConnectCommand { get; }

    public AsyncRelayCommand DisconnectCommand { get; }

    public AsyncRelayCommand SendCommand { get; }

    public RelayCommand ClearLogCommand { get; }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string StatusDetail
    {
        get => _statusDetail;
        set => SetProperty(ref _statusDetail, value);
    }

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

    public string ProtocolBadgeText => "LengthPrefixed";

    public string SerializerBadgeText => "AutoBinary";

    public string RuntimePolicyText => "Strict MVVM + DI + JSON";

    public async ValueTask DisposeAsync()
    {
        _sessionService.LogEmitted -= OnLogEmitted;
        _sessionService.ConnectionStateChanged -= OnConnectionStateChanged;
        await _sessionService.DisposeAsync().ConfigureAwait(false);
    }

    private bool CanConnect()
    {
        return !IsBusy && !IsConnected;
    }

    private bool CanDisconnect()
    {
        return !IsBusy && IsConnected;
    }

    private bool CanSend()
    {
        return !IsBusy && IsConnected;
    }

    private async Task ConnectAsync()
    {
        IsBusy = true;
        StatusText = "Connecting";
        StatusDetail = "Negotiating transport and starting receive loop.";

        try
        {
            var profile = BuildProfile();
            await _sessionService.ConnectAsync(profile).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            StatusText = "Disconnected";
            StatusDetail = "Connection failed before the session came online.";
            AppendLog(new LogEntry(DateTimeOffset.Now, LogSeverity.Error, "Connect failed", exception.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DisconnectAsync()
    {
        IsBusy = true;
        StatusText = "Disconnecting";
        StatusDetail = "Shutting down transport and cleaning background work.";

        try
        {
            await _sessionService.DisconnectAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            AppendLog(new LogEntry(DateTimeOffset.Now, LogSeverity.Error, "Disconnect failed", exception.Message));
            StatusText = "Disconnected";
            StatusDetail = "Disconnect ended with an error.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendAsync()
    {
        IsBusy = true;
        StatusText = "Sending";
        StatusDetail = "Writing the outbound frame to the active transport.";

        try
        {
            await _sessionService.SendAsync(ParseMessageId(), Settings.OutboundBody).ConfigureAwait(false);
            StatusText = "Connected";
            StatusDetail = "Session is online and ready for the next message.";
        }
        catch (Exception exception)
        {
            AppendLog(new LogEntry(DateTimeOffset.Now, LogSeverity.Error, "Send failed", exception.Message));
            StatusText = IsConnected ? "Connected" : "Disconnected";
            StatusDetail = IsConnected
                ? "Session remained online after the send failure."
                : "Session is not active.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearLogs()
    {
        Logs.Clear();
        Logs.Add(new LogEntry(DateTimeOffset.Now, LogSeverity.Info, "Console cleared", "New traffic will appear here."));
    }

    private DeviceProfile BuildProfile()
    {
        var trimmedDeviceId = Settings.DeviceId.Trim();

        return new DeviceProfile
        {
            DeviceId = trimmedDeviceId,
            DisplayName = string.IsNullOrWhiteSpace(Settings.DisplayName) ? trimmedDeviceId : Settings.DisplayName.Trim(),
            Enabled = true,
            Transport = BuildTransportOptions(),
            Protocol = new ProtocolOptions
            {
                Type = "LengthPrefixed",
                MaxFrameLength = 4096,
                UseCrc = false
            },
            Serializer = new SerializerOptions
            {
                Type = "AutoBinary"
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

    private ushort ParseMessageId()
    {
        if (!ushort.TryParse(Settings.OutboundMessageId.Trim(), out var messageId))
        {
            throw new InvalidOperationException("Message Id must be between 0 and 65535.");
        }

        return messageId;
    }

    private void NotifyCommandStateChanged()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SendCommand.NotifyCanExecuteChanged();
    }

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

    private void OnLogEmitted(object? sender, LogEntry entry)
    {
        AppendLog(entry);
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateSnapshot snapshot)
    {
        _uiDispatcher.Enqueue(() =>
        {
            IsConnected = snapshot.IsConnected;
            StatusText = snapshot.StatusText;
            StatusDetail = snapshot.StatusDetail;
        });
    }

    private void AppendLog(LogEntry entry)
    {
        _uiDispatcher.Enqueue(() =>
        {
            Logs.Add(entry);
        });
    }
}
