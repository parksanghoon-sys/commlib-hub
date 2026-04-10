using System.Threading.Channels;
using System.Runtime.ExceptionServices;
using CommLib.Application.Sessions;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Protocol;
using CommLib.Infrastructure.Transport;

namespace CommLib.Infrastructure.Sessions;

/// <summary>
/// 연결된 장치에 대한 전송 생성과 인메모리 세션 등록을 관리합니다.
/// </summary>
public sealed class ConnectionManager : IConnectionManager, IAsyncDisposable
{
    private const int DefaultInboundQueueCapacity = 256;

    private readonly ITransportFactory _transportFactory;
    private readonly IProtocolFactory _protocolFactory;
    private readonly ISerializerFactory _serializerFactory;
    private readonly IConnectionEventSink _eventSink;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly int _inboundQueueCapacity;
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, DeviceConnectionState> _connections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SemaphoreSlim> _deviceOperationGates = new(StringComparer.Ordinal);
    private bool _disposeRequested;

    /// <summary>
    /// <see cref="ConnectionManager"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="transportFactory">장치 전송을 생성하는 transport factory입니다.</param>
    /// <param name="protocolFactory">장치 프로토콜을 생성하는 protocol factory입니다.</param>
    /// <param name="serializerFactory">메시지 serializer를 생성하는 serializer factory입니다.</param>
    public ConnectionManager(
        ITransportFactory transportFactory,
        IProtocolFactory protocolFactory,
        ISerializerFactory serializerFactory,
        IConnectionEventSink? eventSink = null)
        : this(
            transportFactory,
            protocolFactory,
            serializerFactory,
            eventSink,
            static (delay, cancellationToken) => Task.Delay(delay, cancellationToken))
    {
    }

    internal ConnectionManager(
        ITransportFactory transportFactory,
        IProtocolFactory protocolFactory,
        ISerializerFactory serializerFactory,
        IConnectionEventSink? eventSink,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        int inboundQueueCapacity = DefaultInboundQueueCapacity)
    {
        if (inboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inboundQueueCapacity), "Inbound queue capacity must be greater than 0.");
        }

        _transportFactory = transportFactory;
        _protocolFactory = protocolFactory;
        _serializerFactory = serializerFactory;
        _eventSink = eventSink ?? NullConnectionEventSink.Instance;
        _delayAsync = delayAsync;
        _inboundQueueCapacity = inboundQueueCapacity;
    }

    /// <summary>
    /// 지정한 장치 프로필에 대한 전송을 만들고 세션 및 송신기를 등록합니다.
    /// </summary>
    /// <param name="profile">연결할 장치 프로필입니다.</param>
    /// <param name="cancellationToken">연결 작업 취소 토큰입니다.</param>
    /// <returns>등록 작업입니다.</returns>
    public async Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposeRequested();

        var deviceGate = await AcquireDeviceGateAsync(profile.DeviceId, cancellationToken).ConfigureAwait(false);
        DeviceConnectionState? existingState = null;
        DeviceConnectionState? newState = null;

        try
        {
            ThrowIfDisposeRequested();

            var protocol = _protocolFactory.Create(profile.Protocol);
            var serializer = _serializerFactory.Create(profile.Serializer);
            var transport = await CreateOpenedTransportAsync(profile, cancellationToken).ConfigureAwait(false);
            var sender = new TransportMessageSender(new MessageFrameEncoder(serializer, protocol), transport);
            var decoder = new MessageFrameDecoder(protocol, serializer);
            var receiver = new TransportMessageReceiver(decoder, transport);
            var session = new DeviceSession(profile.DeviceId, profile.RequestResponse);

            // �������� ��Ī���� ���� inbound�� ���� bounded queue�� ������ �޸� ������ �����մϴ�.
            var inboundQueue = CreateInboundQueue();
            var receivePumpTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            newState = new DeviceConnectionState(
                session,
                sender,
                receiver,
                transport,
                inboundQueue,
                receivePumpTokenSource);
            existingState = GetConnectionState(profile.DeviceId);

            if (existingState is not null)
            {
                await CloseTransportAsync(profile.DeviceId, existingState.Transport, "disconnect", cancellationToken).ConfigureAwait(false);
                existingState.Session.FailPendingResponses(CreatePendingResponseTerminationException(profile.DeviceId, "disconnect"));
            }

            RegisterConnectionState(profile.DeviceId, newState);
            newState.ReceivePumpTask = RunReceivePumpAsync(
                profile.DeviceId,
                newState,
                newState.InboundQueue.Writer,
                newState.ReceivePumpTokenSource.Token);

            if (existingState is not null)
            {
                StopConnectionState(existingState);
            }
        }
        catch
        {
            if (newState is not null)
            {
                RemoveConnectionState(profile.DeviceId, newState);
                await CleanupConnectionStateAsync(newState).ConfigureAwait(false);
            }

            throw;
        }
        finally
        {
            deviceGate.Release();
        }
    }

    /// <summary>
    /// 지정한 장치 식별자의 연결 리소스와 수신 수명주기를 정리합니다.
    /// </summary>
    /// <param name="deviceId">연결 해제할 장치 식별자입니다.</param>
    /// <param name="cancellationToken">연결 해제 취소 토큰입니다.</param>
    /// <returns>연결 해제 처리 작업입니다.</returns>
    public async Task DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deviceGate = await AcquireDeviceGateAsync(deviceId, cancellationToken).ConfigureAwait(false);
        DeviceConnectionState? state = null;

        try
        {
            state = GetRequiredConnectionState(deviceId);
            await CloseTransportAsync(deviceId, state.Transport, "disconnect", cancellationToken).ConfigureAwait(false);
            state.Session.FailPendingResponses(CreatePendingResponseTerminationException(deviceId, "disconnect"));
            RemoveConnectionState(deviceId, state);
            StopConnectionState(state);
        }
        finally
        {
            deviceGate.Release();
        }
    }

    /// <summary>
    /// 지정한 장치 식별자로 메시지를 전송합니다.
    /// </summary>
    /// <param name="deviceId">메시지를 보낼 장치 식별자입니다.</param>
    /// <param name="message">전송할 메시지입니다.</param>
    /// <param name="cancellationToken">전송 취소 토큰입니다.</param>
    /// <returns>전송 작업입니다.</returns>
    public Task SendAsync(string deviceId, IMessage message, CancellationToken cancellationToken = default)
    {
        var state = GetRequiredConnectionState(deviceId);
        ThrowIfReceivePumpFailed(state);
        return SendFromSessionAsync(state, message, cancellationToken);
    }

    /// <summary>
    /// 지정한 장치 식별자로 다음 inbound 메시지를 수신합니다.
    /// </summary>
    /// <param name="deviceId">메시지를 수신할 장치 식별자입니다.</param>
    /// <param name="cancellationToken">수신 취소 토큰입니다.</param>
    /// <returns>복원된 inbound 메시지입니다.</returns>
    public async Task<IMessage> ReceiveAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var state = GetRequiredConnectionState(deviceId);
        InboundEnvelope envelope;
        try
        {
            envelope = await state.InboundQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException) when (state.ReceivePumpFailure is not null)
        {
            ExceptionDispatchInfo.Capture(state.ReceivePumpFailure).Throw();
            throw;
        }

        if (envelope.Exception is not null)
        {
            throw envelope.Exception;
        }

        return envelope.Message ??
               throw new InvalidOperationException($"Inbound queue for device '{deviceId}' returned an empty envelope.");
    }

    /// <summary>
    /// 장치 식별자로 활성 세션을 조회합니다.
    /// </summary>
    /// <param name="deviceId">조회할 장치 세션 식별자입니다.</param>
    /// <returns>활성 세션이 있으면 반환하고, 없으면 <see langword="null"/>을 반환합니다.</returns>
    public IDeviceSession? GetSession(string deviceId)
    {
        var state = GetConnectionState(deviceId);
        return state is null || state.ReceivePumpFailure is not null
            ? null
            : state.Session;
    }

    /// <summary>
    /// 활성 장치 연결과 수신 수명주기를 모두 정리합니다.
    /// </summary>
    /// <returns>비동기 정리 작업입니다.</returns>
    public async ValueTask DisposeAsync()
    {
        string[] deviceIds;
        lock (_syncRoot)
        {
            _disposeRequested = true;
            deviceIds = _connections.Keys.ToArray();
        }

        List<Exception>? exceptions = null;
        foreach (var deviceId in deviceIds)
        {
            try
            {
                await DisconnectAsync(deviceId).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(exception);
            }
        }

        if (exceptions is null)
        {
            return;
        }

        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        throw new AggregateException("One or more device disconnect operations failed during disposal.", exceptions);
    }

    /// <summary>
    /// 입력 프레임을 디코드하고 필요하면 대기 중인 응답을 완료 처리합니다.
    /// </summary>
    /// <param name="deviceId">프레임을 수신한 장치 식별자입니다.</param>
    /// <param name="buffer">디코드할 입력 버퍼입니다.</param>
    /// <param name="message">복원된 메시지입니다.</param>
    /// <param name="bytesConsumed">소비한 바이트 수입니다.</param>
    /// <returns>완전한 메시지를 복원했으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    public bool TryHandleInboundFrame(string deviceId, ReadOnlySpan<byte> buffer, out IMessage? message, out int bytesConsumed)
    {
        message = null;
        bytesConsumed = 0;

        var state = GetRequiredConnectionState(deviceId);
        ThrowIfReceivePumpFailed(state);

        if (!state.Receiver.TryDecode(buffer, out var decodedMessage, out bytesConsumed))
        {
            return false;
        }

        message = decodedMessage;
        if (message is IResponseMessage response)
        {
            state.Session.TryCompleteResponse(response);
        }

        return true;
    }

    private static async Task SendFromSessionAsync(
        DeviceConnectionState state,
        IMessage message,
        CancellationToken cancellationToken)
    {
        var session = state.Session;
        var result = session.Send(message);
        await result.SendCompletedTask.ConfigureAwait(false);

        if (!session.TryDequeueOutbound(out var outbound) || outbound is null)
        {
            throw new InvalidOperationException($"Session '{session.DeviceId}' did not expose an outbound message.");
        }

        ThrowIfReceivePumpFailed(state);
        await state.Sender.SendAsync(outbound, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunReceivePumpAsync(
        string deviceId,
        DeviceConnectionState state,
        ChannelWriter<InboundEnvelope> inboundWriter,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await state.Receiver.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (message is IResponseMessage response && state.Session.TryCompleteResponse(response))
                {
                    continue;
                }

                await inboundWriter.WriteAsync(new InboundEnvelope(message, null), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
                // ť�� ���� ���� writer�� ����ϹǷ� �߰� transport ���ŵ� �Բ� backpressure�� �޽��ϴ�.
        }
        catch (Exception exception)
        {
            var wrapped = new DeviceConnectionException(deviceId, "receive", exception);
            state.ReceivePumpFailure = wrapped;
            state.Session.FailPendingResponses(wrapped);
            _eventSink.OnOperationFailed(deviceId, "receive", wrapped);
            await TryCloseTransportAsync(state.Transport).ConfigureAwait(false);

            if (!inboundWriter.TryWrite(new InboundEnvelope(null, wrapped)))
            {
                inboundWriter.TryComplete(wrapped);
            }
        }
        finally
        {
            inboundWriter.TryComplete();
        }
    }

    private sealed record InboundEnvelope(IMessage? Message, Exception? Exception);

    private sealed class DeviceConnectionState
    {
        public DeviceConnectionState(
            IDeviceSession session,
            TransportMessageSender sender,
            TransportMessageReceiver receiver,
    private Channel<InboundEnvelope> CreateInboundQueue()
    {
        var options = new BoundedChannelOptions(_inboundQueueCapacity)
        {
            // �޽����� ������� �ʰ� �����ڸ� ������ �޸� ��뷮�� �����մϴ�.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };

        return Channel.CreateBounded<InboundEnvelope>(options);
    }

            ITransport transport,
            Channel<InboundEnvelope> inboundQueue,
            CancellationTokenSource receivePumpTokenSource)
        {
            Session = session;
            Sender = sender;
            Receiver = receiver;
            Transport = transport;
            InboundQueue = inboundQueue;
            ReceivePumpTokenSource = receivePumpTokenSource;
        }

        public IDeviceSession Session { get; }

        public TransportMessageSender Sender { get; }

        public TransportMessageReceiver Receiver { get; }

        public ITransport Transport { get; }

        public Channel<InboundEnvelope> InboundQueue { get; }

        public CancellationTokenSource ReceivePumpTokenSource { get; }

        public Task ReceivePumpTask { get; set; } = Task.CompletedTask;

        public volatile Exception? ReceivePumpFailure;
    }

    private static void DropPendingInbound(Channel<InboundEnvelope> inboundQueue)
    {
        inboundQueue.Writer.TryComplete();
        while (inboundQueue.Reader.TryRead(out _))
        {
        }
    }

        // ���� ������ �ܿ� inbound�� ������ �翬�� �� �� �������� ���� ������ �ʰ� �մϴ�.
    private static async Task TryCloseTransportAsync(ITransport transport)
    {
        try
        {
            await transport.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private static async Task CleanupConnectionStateAsync(DeviceConnectionState state)
    {
        StopConnectionState(state);
        await TryCloseTransportAsync(state.Transport).ConfigureAwait(false);
    }

    private static DeviceConnectionException CreatePendingResponseTerminationException(string deviceId, string operation)
    {
        return new DeviceConnectionException(
            deviceId,
            operation,
            new InvalidOperationException("Device session closed before a pending response arrived."));
    }

    private static void ThrowIfReceivePumpFailed(DeviceConnectionState state)
    {
        if (state.ReceivePumpFailure is null)
        {
            return;
        }

        ExceptionDispatchInfo.Capture(state.ReceivePumpFailure).Throw();
    }

    private static void StopConnectionState(DeviceConnectionState state)
    {
        state.ReceivePumpTokenSource.Cancel();
        state.ReceivePumpTokenSource.Dispose();
        DropPendingInbound(state.InboundQueue);
    }

    private async Task CloseTransportAsync(
        string deviceId,
        ITransport transport,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await transport.CloseAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var wrapped = new DeviceConnectionException(deviceId, operation, exception);
            _eventSink.OnOperationFailed(deviceId, operation, wrapped);
            throw wrapped;
        }
    }

    private async Task<SemaphoreSlim> AcquireDeviceGateAsync(string deviceId, CancellationToken cancellationToken)
    {
        var deviceGate = GetOrCreateDeviceGate(deviceId);
        await deviceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return deviceGate;
    }

    private SemaphoreSlim GetOrCreateDeviceGate(string deviceId)
    {
        lock (_syncRoot)
        {
            if (!_deviceOperationGates.TryGetValue(deviceId, out var deviceGate))
            {
                deviceGate = new SemaphoreSlim(1, 1);
                _deviceOperationGates[deviceId] = deviceGate;
            }

            return deviceGate;
        }
    }

    private DeviceConnectionState GetRequiredConnectionState(string deviceId)
    {
        return GetConnectionState(deviceId) ??
               throw new InvalidOperationException($"No session registered for device '{deviceId}'.");
    }

    private DeviceConnectionState? GetConnectionState(string deviceId)
    {
        lock (_syncRoot)
        {
            _connections.TryGetValue(deviceId, out var state);
            return state;
        }
    }

    private void RegisterConnectionState(string deviceId, DeviceConnectionState state)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposeRequestedLocked();
            _connections[deviceId] = state;
        }
    }

    private void RemoveConnectionState(string deviceId, DeviceConnectionState expectedState)
    {
        lock (_syncRoot)
        {
            if (_connections.TryGetValue(deviceId, out var state) && ReferenceEquals(state, expectedState))
            {
                _connections.Remove(deviceId);
            }
        }
    }

    private void ThrowIfDisposeRequested()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposeRequestedLocked();
        }
    }

    private void ThrowIfDisposeRequestedLocked()
    {
        if (_disposeRequested)
        {
            throw new ObjectDisposedException(nameof(ConnectionManager));
        }
    }

    private async Task<ITransport> CreateOpenedTransportAsync(DeviceProfile profile, CancellationToken cancellationToken)
    {
        var totalAttempts = GetTotalConnectAttempts(profile.Reconnect);

        for (var attempt = 1; attempt <= totalAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _eventSink.OnConnectAttempt(profile.DeviceId, attempt, totalAttempts);

            ITransport? transport = null;
            try
            {
                transport = _transportFactory.Create(profile.Transport);
                await transport.OpenAsync(cancellationToken).ConfigureAwait(false);
                _eventSink.OnConnectSucceeded(profile.DeviceId, attempt);
                return transport;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (transport is not null)
                {
                    await TryCloseTransportAsync(transport).ConfigureAwait(false);
                }

                throw;
            }
            catch (Exception exception)
            {
                if (transport is not null)
                {
                    await TryCloseTransportAsync(transport).ConfigureAwait(false);
                }

                if (attempt >= totalAttempts)
                {
                    var wrapped = new DeviceConnectionException(profile.DeviceId, "connect", exception);
                    _eventSink.OnOperationFailed(profile.DeviceId, "connect", wrapped);
                    throw wrapped;
                }

                var delay = GetRetryDelay(profile.Reconnect, attempt);
                _eventSink.OnConnectRetryScheduled(profile.DeviceId, attempt, delay, exception);
                await _delayAsync(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException($"Device '{profile.DeviceId}' exhausted its connection attempts unexpectedly.");
    }

    private static int GetTotalConnectAttempts(ReconnectOptions reconnect)
    {
        if (reconnect.MaxAttempts <= 0 || IsNoRetryPolicy(reconnect.Type))
        {
            return 1;
        }

        return checked(reconnect.MaxAttempts + 1);
    }

    private static TimeSpan GetRetryDelay(ReconnectOptions reconnect, int failedAttemptNumber)
    {
        if (IsLinearPolicy(reconnect.Type))
        {
            return TimeSpan.FromMilliseconds(reconnect.IntervalMs);
        }

        var exponent = Math.Max(0, failedAttemptNumber - 1);
        var computedDelay = reconnect.BaseDelayMs * Math.Pow(2, exponent);
        var boundedDelay = Math.Min((long)Math.Ceiling(computedDelay), reconnect.MaxDelayMs);
        return TimeSpan.FromMilliseconds(boundedDelay);
    }

    private static bool IsNoRetryPolicy(string type)
    {
        return type.Equals("None", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLinearPolicy(string type)
    {
        return type.Equals("Linear", StringComparison.OrdinalIgnoreCase);
    }
}
