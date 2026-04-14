using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Examples.WinUI.Models;
using CommLib.Infrastructure.Sessions;

namespace CommLib.Examples.WinUI.Services;

public sealed class DeviceLabSessionService : IDeviceLabSessionService
{
    private readonly IAppLocalizer _localizer;
    private readonly ITransportFactory _transportFactory;
    private readonly IProtocolFactory _protocolFactory;
    private readonly ISerializerFactory _serializerFactory;
    // connect / disconnect / send가 서로 겹치면 receive loop 정리와 상태 전파가 꼬이기 쉬워서
    // 세션 수명주기 관련 작업은 하나의 gate로 직렬화한다.
    private readonly SemaphoreSlim _sessionGate = new(1, 1);

    private ConnectionManager? _manager;
    private CancellationTokenSource? _receiveLoopCts;
    private Task? _receiveLoopTask;
    private string? _connectedDeviceId;
    private BitFieldPayloadSchema? _activeBitFieldSchema;
    private bool _hasState;
    private bool _isConnectedState;
    private string _statusTextKey = "session.state.disconnected";
    private string _statusDetailKey = "session.detail.readyNextTransport";
    private object?[] _statusTextArgs = [];
    private object?[] _statusDetailArgs = [];

    public DeviceLabSessionService(
        ITransportFactory transportFactory,
        IProtocolFactory protocolFactory,
        ISerializerFactory serializerFactory,
        IAppLocalizer localizer)
    {
        _transportFactory = transportFactory;
        _protocolFactory = protocolFactory;
        _serializerFactory = serializerFactory;
        _localizer = localizer;
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    public event EventHandler<LogEntry>? LogEmitted;

    public event EventHandler<ConnectionStateSnapshot>? ConnectionStateChanged;

    public async Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // 새 연결 요청은 항상 이전 연결을 먼저 깨끗이 정리한 뒤 시작한다.
            // 이렇게 하면 Device Lab에서 transport를 바꿔 가며 반복 연결해도 상태가 누적되지 않는다.
            await DisconnectCoreAsync(CancellationToken.None, emitOfflineState: false).ConfigureAwait(false);
            DeviceProfileValidator.ValidateAndThrow(profile);

            var manager = new ConnectionManager(
                _transportFactory,
                _protocolFactory,
                _serializerFactory,
                new SessionConnectionEventSink(this));

            try
            {
                await manager.ConnectAsync(profile, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await manager.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            _manager = manager;
            _connectedDeviceId = profile.DeviceId;
            _activeBitFieldSchema = profile.Serializer.BitFieldSchema;
            _receiveLoopCts = new CancellationTokenSource();
            // 실제 수신은 백그라운드 루프가 담당하고, 결과만 log/state 이벤트로 ViewModel에 흘려보낸다.
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(profile.DeviceId, _receiveLoopCts.Token));

            var (detailKey, detailArgs) = DescribeTransport(profile.Transport);
            SetState(
                true,
                "session.state.connected",
                detailKey,
                statusTextArgs: [profile.DisplayName],
                statusDetailArgs: detailArgs);

            EmitLocalizedLog(
                LogSeverity.Success,
                "session.log.sessionOnline.title",
                "session.log.sessionOnline.message",
                profile.DeviceId);
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var hadSession = _manager is not null || _connectedDeviceId is not null;
            await DisconnectCoreAsync(cancellationToken, emitOfflineState: hadSession).ConfigureAwait(false);
            if (hadSession)
            {
                EmitLocalizedLog(
                    LogSeverity.Info,
                    "session.log.sessionOffline.title",
                    "session.log.sessionOffline.message");
            }
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public async Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_manager is null || _connectedDeviceId is null)
            {
                throw new InvalidOperationException(_localizer.Get("session.error.noActiveSession"));
            }

            await _manager.SendAsync(_connectedDeviceId, message, cancellationToken).ConfigureAwait(false);
            var body = FormatMessageBodyForLog(message);
            EmitLocalizedLog(
                LogSeverity.Info,
                "session.log.outbound.title",
                "session.log.outbound.message",
                message.MessageId,
                body);
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sessionGate.WaitAsync().ConfigureAwait(false);

        try
        {
            await DisconnectCoreAsync(CancellationToken.None, emitOfflineState: false).ConfigureAwait(false);
            _localizer.LanguageChanged -= OnLanguageChanged;
        }
        finally
        {
            _sessionGate.Release();
            _sessionGate.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var manager = _manager;
                if (manager is null)
                {
                    return;
                }

                var message = await manager.ReceiveAsync(deviceId, cancellationToken).ConfigureAwait(false);
                var body = FormatMessageBodyForLog(message);
                EmitLocalizedLog(
                    LogSeverity.Success,
                    "session.log.inbound.title",
                    "session.log.inbound.message",
                    message.MessageId,
                    body);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            EmitLog(LogSeverity.Error, _localizer.Get("session.log.receiveLoopStopped.title"), exception.Message);
            await _sessionGate.WaitAsync().ConfigureAwait(false);

            try
            {
                // 현재 receive loop 자신이 종료 경로를 밟는 상황이라 여기서 _receiveLoopTask를 다시 await 하면
                // 자기 자신을 기다리는 꼴이 될 수 있다. 그래서 skipReceiveLoopAwait로 우회한다.
                await DisconnectCoreAsync(CancellationToken.None, emitOfflineState: true, skipReceiveLoopAwait: true)
                    .ConfigureAwait(false);
            }
            finally
            {
                _sessionGate.Release();
            }
        }
    }

    private async Task DisconnectCoreAsync(
        CancellationToken cancellationToken,
        bool emitOfflineState,
        bool skipReceiveLoopAwait = false)
    {
        if (_receiveLoopCts is not null)
        {
            _receiveLoopCts.Cancel();
        }

        if (!skipReceiveLoopAwait && _receiveLoopTask is not null)
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            finally
            {
                _manager = null;
                _connectedDeviceId = null;
                _activeBitFieldSchema = null;
            }
        }
        else
        {
            _connectedDeviceId = null;
            _activeBitFieldSchema = null;
        }

        if (emitOfflineState)
        {
            SetState(false, "session.state.disconnected", "session.detail.readyNextTransport");
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        EmitCurrentState();
    }

    private void EmitLog(LogSeverity severity, string title, string message)
    {
        LogEmitted?.Invoke(this, new LogEntry(DateTimeOffset.Now, severity, title, message));
    }

    private void EmitLocalizedLog(LogSeverity severity, string titleKey, string messageKey, params object?[] args)
    {
        EmitLog(severity, _localizer.Get(titleKey), _localizer.Format(messageKey, args));
    }

    private string FormatMessageBodyForLog(IMessage message)
    {
        var body = MessagePayloadFormatter.FormatBody(message);

        if (MessagePayloadFormatter.TryFormatBitFieldSummary(message, _activeBitFieldSchema, out var summary, out var error))
        {
            return AppendPayloadSuffix(body, _localizer.Format("session.payload.fieldsSuffix", summary));
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            return AppendPayloadSuffix(body, _localizer.Format("session.payload.schemaErrorSuffix", error));
        }

        return body;
    }

    private void SetState(
        bool isConnected,
        string statusTextKey,
        string statusDetailKey,
        object?[]? statusTextArgs = null,
        object?[]? statusDetailArgs = null)
    {
        // 완성된 문자열이 아니라 "key + format args"를 저장해야
        // 앱 언어가 바뀔 때 마지막 연결 상태를 현재 언어로 다시 렌더링할 수 있다.
        _hasState = true;
        _isConnectedState = isConnected;
        _statusTextKey = statusTextKey;
        _statusDetailKey = statusDetailKey;
        _statusTextArgs = statusTextArgs ?? [];
        _statusDetailArgs = statusDetailArgs ?? [];
        EmitCurrentState();
    }

    private void EmitCurrentState()
    {
        if (!_hasState)
        {
            return;
        }

        ConnectionStateChanged?.Invoke(
            this,
            new ConnectionStateSnapshot(
                _isConnectedState,
                _localizer.Format(_statusTextKey, _statusTextArgs),
                _localizer.Format(_statusDetailKey, _statusDetailArgs)));
    }

    private static (string DetailKey, object?[] DetailArgs) DescribeTransport(TransportOptions transport)
    {
        return transport switch
        {
            TcpClientTransportOptions tcp => ("transport.detail.tcp", [tcp.Host ?? string.Empty, tcp.Port]),
            UdpTransportOptions udp => ("transport.detail.udp", [udp.LocalPort, udp.RemoteHost ?? string.Empty, udp.RemotePort]),
            MulticastTransportOptions multicast => ("transport.detail.multicast", [multicast.GroupAddress ?? string.Empty, multicast.Port]),
            SerialTransportOptions serial => ("transport.detail.serial", [serial.PortName ?? string.Empty, serial.BaudRate]),
            _ => ("transport.detail.generic", [transport.Type ?? string.Empty])
        };
    }

    private static string AppendPayloadSuffix(string body, string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
        {
            return body;
        }

        return string.IsNullOrEmpty(body)
            ? suffix
            : $"{body} | {suffix}";
    }

    private sealed class SessionConnectionEventSink(DeviceLabSessionService owner) : IConnectionEventSink
    {
        // ConnectionManager는 인프라 계층 이벤트만 알면 되고,
        // 실제 UI 친화적 메시지 구성은 WinUI 쪽 서비스가 담당하도록 얇은 어댑터를 둔다.
        private readonly DeviceLabSessionService _owner = owner;

        public void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts)
        {
            _owner.EmitLocalizedLog(
                LogSeverity.Info,
                "session.event.connectAttempt.title",
                "session.event.connectAttempt.message",
                deviceId,
                attemptNumber,
                totalAttempts);
        }

        public void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception)
        {
            _owner.EmitLocalizedLog(
                LogSeverity.Warning,
                "session.event.retryScheduled.title",
                "session.event.retryScheduled.message",
                deviceId,
                attemptNumber,
                delay.TotalMilliseconds,
                exception.Message);
        }

        public void OnConnectSucceeded(string deviceId, int attemptNumber)
        {
            _owner.EmitLocalizedLog(
                LogSeverity.Success,
                "session.event.connectSucceeded.title",
                "session.event.connectSucceeded.message",
                deviceId,
                attemptNumber);
        }

        public void OnOperationFailed(string deviceId, string operation, Exception exception)
        {
            _owner.EmitLocalizedLog(
                LogSeverity.Error,
                "session.event.operationFailed.title",
                "session.event.operationFailed.message",
                deviceId,
                operation,
                exception.Message);
        }
    }
}
