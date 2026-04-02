using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CommLib.Examples.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ObservableCollection<string> _logs = new();
    private ConnectionManager? _manager;
    private CancellationTokenSource? _receiveLoopCts;
    private Task? _receiveLoopTask;
    private string? _connectedDeviceId;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        Title = "CommLib Device Lab";
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ??
                           throw new InvalidOperationException("No DispatcherQueue is available.");
        LogListView.ItemsSource = _logs;
        UpdateTransportSectionVisibility();
        Closed += MainWindow_Closed;
        AppendLog("Ready. Choose a transport and connect.");
    }

    private bool IsConnected
    {
        get { return _manager is not null && _connectedDeviceId is not null; }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectButton.IsEnabled = false;

        try
        {
            await DisconnectManagerAsync().ConfigureAwait(false);

            var profile = BuildProfile();
            DeviceProfileValidator.ValidateAndThrow(profile);

            var manager = new ConnectionManager(
                new TransportFactory(),
                new ProtocolFactory(),
                new SerializerFactory(),
                new UiConnectionEventSink(AppendLog));

            await manager.ConnectAsync(profile).ConfigureAwait(false);
            _manager = manager;
            _connectedDeviceId = profile.DeviceId;
            StartReceiveLoop(profile.DeviceId);
            SetConnectedState(true, "Connected");
            AppendLog("Connected to " + DescribeTransport(profile.Transport));
        }
        catch (Exception exception)
        {
            await DisconnectManagerAsync().ConfigureAwait(false);
            SetConnectedState(false, "Disconnected");
            AppendLog("Connect failed: " + exception.Message);
        }
        finally
        {
            ConnectButton.IsEnabled = !IsConnected;
        }
    }

    private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await DisconnectManagerAsync().ConfigureAwait(false);
            SetConnectedState(false, "Disconnected");
            AppendLog("Disconnected.");
        }
        catch (Exception exception)
        {
            SetConnectedState(false, "Disconnected");
            AppendLog("Disconnect failed: " + exception.Message);
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (_manager is null || _connectedDeviceId is null)
        {
            AppendLog("Connect before sending.");
            return;
        }

        SendButton.IsEnabled = false;

        try
        {
            var message = new MessageModel(ParseUShort(MessageIdTextBox, "Message Id"), MessageBodyTextBox.Text);
            await _manager.SendAsync(_connectedDeviceId, message).ConfigureAwait(false);
            AppendLog("Sent: id=" + message.MessageId + ", body=\"" + message.Body + "\"");
        }
        catch (Exception exception)
        {
            AppendLog("Send failed: " + exception.Message);
        }
        finally
        {
            if (IsConnected)
            {
                SendButton.IsEnabled = true;
            }
        }
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _logs.Clear();
        AppendLog("Log cleared.");
    }

    private void TransportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTransportSectionVisibility();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        _ = DisconnectManagerAsync();
    }

    private DeviceProfile BuildProfile()
    {
        var deviceId = DeviceIdTextBox.Text.Trim();
        return new DeviceProfile
        {
            DeviceId = deviceId,
            DisplayName = string.IsNullOrWhiteSpace(DisplayNameTextBox.Text) ? deviceId : DisplayNameTextBox.Text.Trim(),
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
                DefaultTimeoutMs = ParseInt(DefaultTimeoutTextBox, "Default Timeout"),
                MaxPendingRequests = ParseInt(MaxPendingTextBox, "Max Pending Requests")
            }
        };
    }

    private TransportOptions BuildTransportOptions()
    {
        switch (GetSelectedTransportType())
        {
            case "tcp":
                return new TcpClientTransportOptions
                {
                    Type = "TcpClient",
                    Host = TcpHostTextBox.Text.Trim(),
                    Port = ParseInt(TcpPortTextBox, "TCP Port"),
                    ConnectTimeoutMs = ParseInt(TcpConnectTimeoutTextBox, "TCP Connect Timeout"),
                    BufferSize = ParseInt(TcpBufferSizeTextBox, "TCP Buffer Size"),
                    NoDelay = TcpNoDelayCheckBox.IsChecked == true
                };
            case "udp":
                return new UdpTransportOptions
                {
                    Type = "Udp",
                    LocalPort = ParseInt(UdpLocalPortTextBox, "UDP Local Port"),
                    RemoteHost = NullIfWhiteSpace(UdpRemoteHostTextBox.Text),
                    RemotePort = ParseNullableInt(UdpRemotePortTextBox, "UDP Remote Port")
                };
            case "multicast":
                return new MulticastTransportOptions
                {
                    Type = "Multicast",
                    GroupAddress = MulticastGroupTextBox.Text.Trim(),
                    Port = ParseInt(MulticastPortTextBox, "Multicast Port"),
                    Ttl = ParseInt(MulticastTtlTextBox, "Multicast TTL"),
                    LocalInterface = NullIfWhiteSpace(MulticastLocalInterfaceTextBox.Text),
                    Loopback = MulticastLoopbackCheckBox.IsChecked == true
                };
            case "serial":
                return new SerialTransportOptions
                {
                    Type = "Serial",
                    PortName = SerialPortNameTextBox.Text.Trim(),
                    BaudRate = ParseInt(SerialBaudRateTextBox, "Serial Baud Rate"),
                    DataBits = ParseInt(SerialDataBitsTextBox, "Serial Data Bits"),
                    Parity = GetComboBoxText(SerialParityComboBox),
                    StopBits = GetComboBoxText(SerialStopBitsComboBox),
                    HalfDuplex = SerialHalfDuplexCheckBox.IsChecked == true,
                    TurnGapMs = ParseInt(SerialTurnGapTextBox, "Serial Turn Gap"),
                    ReadBufferSize = ParseInt(SerialReadBufferTextBox, "Serial Read Buffer Size"),
                    WriteBufferSize = ParseInt(SerialWriteBufferTextBox, "Serial Write Buffer Size")
                };
            default:
                throw new InvalidOperationException("Unsupported transport selection.");
        }
    }

    private void StartReceiveLoop(string deviceId)
    {
        if (_receiveLoopCts is not null)
        {
            _receiveLoopCts.Cancel();
            _receiveLoopCts.Dispose();
        }

        _receiveLoopCts = new CancellationTokenSource();
        _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(deviceId, _receiveLoopCts.Token));
    }

    private async Task ReceiveLoopAsync(string deviceId, CancellationToken cancellationToken)
    {
        if (_manager is null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _manager.ReceiveAsync(deviceId, cancellationToken).ConfigureAwait(false);
                var body = message is IMessageBody bodyMessage ? bodyMessage.Body : string.Empty;
                AppendLog("Received: id=" + message.MessageId + ", body=\"" + body + "\"");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            AppendLog("Receive loop stopped: " + exception.Message);
            SetConnectedState(false, "Disconnected");
        }
    }

    private async Task DisconnectManagerAsync()
    {
        if (_receiveLoopCts is not null)
        {
            _receiveLoopCts.Cancel();
        }

        if (_receiveLoopTask is not null)
        {
            try
            {
                await _receiveLoopTask.ConfigureAwait(false);
            }
            catch
            {
            }
        }

        _receiveLoopTask = null;

        if (_receiveLoopCts is not null)
        {
            _receiveLoopCts.Dispose();
            _receiveLoopCts = null;
        }

        if (_manager is not null)
        {
            try
            {
                await _manager.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _manager = null;
                _connectedDeviceId = null;
            }
        }

        QueueUi(delegate { ConnectButton.IsEnabled = true; });
    }

    private void SetConnectedState(bool connected, string status)
    {
        QueueUi(delegate
        {
            StatusTextBlock.Text = status;
            ConnectButton.IsEnabled = !connected;
            DisconnectButton.IsEnabled = connected;
            SendButton.IsEnabled = connected;
        });
    }

    private void UpdateTransportSectionVisibility()
    {
        var selected = GetSelectedTransportType();
        TcpSectionBorder.Visibility = selected == "tcp" ? Visibility.Visible : Visibility.Collapsed;
        UdpSectionBorder.Visibility = selected == "udp" ? Visibility.Visible : Visibility.Collapsed;
        MulticastSectionBorder.Visibility = selected == "multicast" ? Visibility.Visible : Visibility.Collapsed;
        SerialSectionBorder.Visibility = selected == "serial" ? Visibility.Visible : Visibility.Collapsed;
    }

    private string GetSelectedTransportType()
    {
        var item = TransportTypeComboBox.SelectedItem as ComboBoxItem;
        return item?.Tag?.ToString() ?? "tcp";
    }

    private void AppendLog(string message)
    {
        QueueUi(delegate
        {
            var entry = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message;
            _logs.Add(entry);
            LogListView.ScrollIntoView(entry);
        });
    }

    private void QueueUi(Action action)
    {
        if (!_dispatcherQueue.TryEnqueue(delegate { action(); }))
        {
            action();
        }
    }

    private static string DescribeTransport(TransportOptions options)
    {
        if (options is TcpClientTransportOptions tcp)
        {
            return "TCP " + tcp.Host + ":" + tcp.Port;
        }

        if (options is UdpTransportOptions udp)
        {
            return "UDP local=" + udp.LocalPort + ", remote=" + udp.RemoteHost + ":" + udp.RemotePort;
        }

        if (options is MulticastTransportOptions multicast)
        {
            return "Multicast " + multicast.GroupAddress + ":" + multicast.Port;
        }

        if (options is SerialTransportOptions serial)
        {
            return "Serial " + serial.PortName + " @ " + serial.BaudRate;
        }

        return options.Type;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int ParseInt(TextBox textBox, string fieldName)
    {
        int value;
        if (!int.TryParse(textBox.Text.Trim(), out value))
        {
            throw new InvalidOperationException(fieldName + " must be an integer.");
        }

        return value;
    }

    private static int? ParseNullableInt(TextBox textBox, string fieldName)
    {
        var trimmed = textBox.Text.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        int value;
        if (!int.TryParse(trimmed, out value))
        {
            throw new InvalidOperationException(fieldName + " must be an integer.");
        }

        return value;
    }

    private static ushort ParseUShort(TextBox textBox, string fieldName)
    {
        ushort value;
        if (!ushort.TryParse(textBox.Text.Trim(), out value))
        {
            throw new InvalidOperationException(fieldName + " must be between 0 and 65535.");
        }

        return value;
    }

    private static string GetComboBoxText(ComboBox comboBox)
    {
        var item = comboBox.SelectedItem as ComboBoxItem;
        if (item?.Content is string text)
        {
            return text;
        }

        throw new InvalidOperationException("A combo box selection is required.");
    }

    private sealed class UiConnectionEventSink : IConnectionEventSink
    {
        private readonly Action<string> _appendLog;

        public UiConnectionEventSink(Action<string> appendLog)
        {
            _appendLog = appendLog;
        }

        public void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts)
        {
            _appendLog("Connect attempt: " + deviceId + " (" + attemptNumber + "/" + totalAttempts + ")");
        }

        public void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception)
        {
            _appendLog("Retry scheduled: " + deviceId + ", attempt=" + attemptNumber + ", delay=" + delay.TotalMilliseconds.ToString("0") + " ms, reason=" + exception.Message);
        }

        public void OnConnectSucceeded(string deviceId, int attemptNumber)
        {
            _appendLog("Connect succeeded: " + deviceId + ", attempt=" + attemptNumber);
        }

        public void OnOperationFailed(string deviceId, string operation, Exception exception)
        {
            _appendLog("Operation failed: " + deviceId + ", operation=" + operation + ", reason=" + exception.Message);
        }
    }
}
