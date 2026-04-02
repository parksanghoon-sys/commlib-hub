using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Examples.WinUI.Models;
using CommLib.Infrastructure.Sessions;

namespace CommLib.Examples.WinUI.Services;

public sealed class DeviceLabSessionService(
    ITransportFactory transportFactory,
    IProtocolFactory protocolFactory,
    ISerializerFactory serializerFactory) : IDeviceLabSessionService
{
    private readonly ITransportFactory _transportFactory = transportFactory;
    private readonly IProtocolFactory _protocolFactory = protocolFactory;
    private readonly ISerializerFactory _serializerFactory = serializerFactory;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);

    private ConnectionManager? _manager;
    private CancellationTokenSource? _receiveLoopCts;
    private Task? _receiveLoopTask;
    private string? _connectedDeviceId;
    private string? _connectedDisplayName;

    public event EventHandler<LogEntry>? LogEmitted;

    public event EventHandler<ConnectionStateSnapshot>? ConnectionStateChanged;

    public async Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await DisconnectCoreAsync(CancellationToken.None, emitOfflineState: false).ConfigureAwait(false);
            DeviceProfileValidator.ValidateAndThrow(profile);

            var manager = new ConnectionManager(
                _transportFactory,
                _protocolFactory,
                _serializerFactory,
                new SessionConnectionEventSink(EmitLog));

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
            _connectedDisplayName = profile.DisplayName;
            _receiveLoopCts = new CancellationTokenSource();
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(profile.DeviceId, _receiveLoopCts.Token));

            EmitState(
                true,
                $"Connected: {profile.DisplayName}",
                DescribeTransport(profile.Transport));

            EmitLog(LogSeverity.Success, "Session online", $"{profile.DeviceId} is ready for traffic.");
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
                EmitLog(LogSeverity.Info, "Session offline", "Transport closed and receive loop stopped.");
            }
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public async Task SendAsync(ushort messageId, string body, CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_manager is null || _connectedDeviceId is null)
            {
                throw new InvalidOperationException("No active device session exists.");
            }

            var message = new MessageModel(messageId, body);
            await _manager.SendAsync(_connectedDeviceId, message, cancellationToken).ConfigureAwait(false);
            EmitLog(LogSeverity.Info, "Outbound message", $"id={messageId}, body=\"{body}\"");
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
                var body = message is IMessageBody bodyMessage ? bodyMessage.Body : string.Empty;
                EmitLog(LogSeverity.Success, "Inbound message", $"id={message.MessageId}, body=\"{body}\"");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            EmitLog(LogSeverity.Error, "Receive loop stopped", exception.Message);
            await _sessionGate.WaitAsync().ConfigureAwait(false);

            try
            {
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
                _connectedDisplayName = null;
            }
        }
        else
        {
            _connectedDeviceId = null;
            _connectedDisplayName = null;
        }

        if (emitOfflineState)
        {
            EmitState(false, "Disconnected", "Ready for the next transport session.");
        }
    }

    private void EmitState(bool isConnected, string statusText, string statusDetail)
    {
        ConnectionStateChanged?.Invoke(this, new ConnectionStateSnapshot(isConnected, statusText, statusDetail));
    }

    private void EmitLog(LogSeverity severity, string title, string message)
    {
        LogEmitted?.Invoke(this, new LogEntry(DateTimeOffset.Now, severity, title, message));
    }

    private static string DescribeTransport(TransportOptions transport)
    {
        return transport switch
        {
            TcpClientTransportOptions tcp => $"TCP {tcp.Host}:{tcp.Port}",
            UdpTransportOptions udp => $"UDP local={udp.LocalPort}, remote={udp.RemoteHost}:{udp.RemotePort}",
            MulticastTransportOptions multicast => $"Multicast {multicast.GroupAddress}:{multicast.Port}",
            SerialTransportOptions serial => $"Serial {serial.PortName} @ {serial.BaudRate}",
            _ => transport.Type
        };
    }

    private sealed class SessionConnectionEventSink(Action<LogSeverity, string, string> emitLog) : IConnectionEventSink
    {
        private readonly Action<LogSeverity, string, string> _emitLog = emitLog;

        public void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts)
        {
            _emitLog(LogSeverity.Info, "Connect attempt", $"{deviceId} ({attemptNumber}/{totalAttempts})");
        }

        public void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception)
        {
            _emitLog(
                LogSeverity.Warning,
                "Retry scheduled",
                $"{deviceId} attempt {attemptNumber} failed, retry in {delay.TotalMilliseconds:0} ms: {exception.Message}");
        }

        public void OnConnectSucceeded(string deviceId, int attemptNumber)
        {
            _emitLog(LogSeverity.Success, "Connect succeeded", $"{deviceId} connected on attempt {attemptNumber}.");
        }

        public void OnOperationFailed(string deviceId, string operation, Exception exception)
        {
            _emitLog(LogSeverity.Error, "Connection operation failed", $"{deviceId} {operation}: {exception.Message}");
        }
    }
}
