using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using CommLib.Application.Configuration;
using CommLib.Application.Sessions;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Protocol;
using CommLib.Infrastructure.Transport;

namespace CommLib.Infrastructure.Sessions;

/// <summary>
/// 장치별 연결 생성, 세션 등록, 수신 펌프 수명주기를 관리합니다.
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
    private readonly Dictionary<string, DeviceOperationGate> _deviceOperationGates = new(StringComparer.Ordinal);
    private bool _disposeRequested;

    /// <summary>
    /// <see cref="ConnectionManager"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="transportFactory">장치 전송을 생성하는 transport factory입니다.</param>
    /// <param name="protocolFactory">장치 프로토콜을 생성하는 protocol factory입니다.</param>
    /// <param name="serializerFactory">메시지 serializer를 생성하는 serializer factory입니다.</param>
    /// <param name="eventSink">연결 수명주기 이벤트를 관찰할 선택적 sink입니다.</param>
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

    /// <summary>
    /// 지정한 inbound queue capacity로 <see cref="ConnectionManager"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="transportFactory">장치 전송을 생성하는 transport factory입니다.</param>
    /// <param name="protocolFactory">장치 프로토콜을 생성하는 protocol factory입니다.</param>
    /// <param name="serializerFactory">메시지 serializer를 생성하는 serializer factory입니다.</param>
    /// <param name="eventSink">연결 이벤트를 관측하는 sink입니다.</param>
    /// <param name="inboundQueueCapacity">장치별 비요청 inbound queue capacity입니다.</param>
    public ConnectionManager(
        ITransportFactory transportFactory,
        IProtocolFactory protocolFactory,
        ISerializerFactory serializerFactory,
        IConnectionEventSink? eventSink,
        int inboundQueueCapacity)
        : this(
            transportFactory,
            protocolFactory,
            serializerFactory,
            eventSink,
            static (delay, cancellationToken) => Task.Delay(delay, cancellationToken),
            inboundQueueCapacity)
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
    /// 지정한 장치 프로필로 연결을 만들고 세션 및 수신 펌프를 등록합니다.
    /// </summary>
    /// <param name="profile">연결할 장치 프로필입니다.</param>
    /// <param name="cancellationToken">연결 작업 취소 토큰입니다.</param>
    /// <returns>연결 등록 작업입니다.</returns>
    public async Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposeRequested();

        // 직접 IConnectionManager를 호출하는 경로도 bootstrap과 같은 검증 규칙을 강제합니다.
        DeviceProfileValidator.ValidateAndThrow(profile);

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

            // 응답으로 매칭되지 않은 inbound는 별도 bounded queue에 보관해 메모리 상한을 유지합니다.
            var inboundQueue = CreateInboundQueue();
            var receivePumpTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            newState = new DeviceConnectionState(
                session,
                sender,
                receiver,
                transport,
                inboundQueue,
                receivePumpTokenSource);
            newState.PublicSession = new ConnectedDeviceSession(this, newState);
            existingState = GetConnectionState(profile.DeviceId);

            if (existingState is not null)
            {
                // 같은 장치를 다시 연결할 때는 이전 세션의 pending 작업을 명시적으로 종료시킵니다.
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
            ReleaseDeviceGate(profile.DeviceId, deviceGate);
        }
    }

    /// <summary>
    /// 지정한 장치의 연결 리소스와 수신 수명주기를 정리합니다.
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
            ReleaseDeviceGate(deviceId, deviceGate);
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
        return SendMessageAsync(state, message, cancellationToken);
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
    /// 지정한 장치 식별자로 활성 세션을 조회합니다.
    /// </summary>
    /// <param name="deviceId">조회할 장치 식별자입니다.</param>
    /// <returns>활성 세션이 있으면 반환하고, 없으면 <see langword="null"/>을 반환합니다.</returns>
    public IDeviceSession? GetSession(string deviceId)
    {
        var state = GetConnectionState(deviceId);
        return state is null || state.ReceivePumpFailure is not null
            ? null
            : state.PublicSession;
    }

    /// <summary>
    /// 활성 연결과 수신 수명주기를 모두 정리합니다.
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
    internal bool TryHandleInboundFrame(string deviceId, ReadOnlySpan<byte> buffer, out IMessage? message, out int bytesConsumed)
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

    /// <summary>
    /// 지정한 연결 상태를 기준으로 메시지를 직접 transport 송신 경로에 전달합니다.
    /// </summary>
    /// <param name="state">메시지를 보낼 현재 장치 연결 상태입니다.</param>
    /// <param name="message">전송할 메시지입니다.</param>
    /// <param name="cancellationToken">송신 취소 토큰입니다.</param>
    /// <returns>transport 송신이 끝나면 완료되는 작업입니다.</returns>
    private async Task SendMessageAsync(
        DeviceConnectionState state,
        IMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // 1. 오래된 facade가 교체된 세션을 붙잡고 있을 수 있으므로 현재 연결 상태와 같은 인스턴스인지 확인합니다.
        ThrowIfConnectionStateInactive(state);

        // 2. receive pump가 이미 실패한 연결이면 저장된 실패를 그대로 전파해 이후 송신을 막습니다.
        ThrowIfReceivePumpFailed(state);

        // 3. 세션 outbound 큐를 경유하지 않고 조립된 sender로 즉시 frame encode와 transport send를 수행합니다.
        await state.Sender.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 공개 세션 facade의 요청/응답 송신 호출을 pending 등록과 실제 transport 송신으로 연결합니다.
    /// </summary>
    /// <typeparam name="TRequest">전송할 요청 메시지 형식입니다.</typeparam>
    /// <typeparam name="TResponse">기다릴 응답 메시지 형식입니다.</typeparam>
    /// <param name="state">요청을 보낼 현재 장치 연결 상태입니다.</param>
    /// <param name="request">전송할 요청 메시지입니다.</param>
    /// <param name="timeout">요청별 응답 timeout입니다.</param>
    /// <returns>송신 완료와 응답 완료를 각각 관찰할 수 있는 결과입니다.</returns>
    private ISendResult<TResponse> SendRequestFromSession<TRequest, TResponse>(
        DeviceConnectionState state,
        TRequest request,
        TimeSpan? timeout)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. pending 등록 전에 세션이 아직 활성인지 확인해 닫힌 세션에 요청을 남기지 않습니다.
        try
        {
            ThrowIfConnectionStateInactive(state);
            ThrowIfReceivePumpFailed(state);
        }
        catch (Exception exception)
        {
            return CreateFailedSendResult<TResponse>(exception);
        }

        // 2. transport 송신보다 먼저 pending을 등록해야 빠른 응답이 와도 receive pump가 놓치지 않습니다.
        if (!state.Session.TryRegisterPendingResponse<TRequest, TResponse>(
                request,
                timeout,
                out var responseTask,
                out var failure))
        {
            return CreateFailedSendResult<TResponse>(failure!);
        }

        // 3. 등록된 요청만 실제로 송신합니다. 송신 실패 시 SendRegisteredRequestAsync가 pending도 실패 처리합니다.
        var sendTask = SendRegisteredRequestAsync(state, request);
        return new SendResult<TResponse>(sendTask, responseTask);
    }

    /// <summary>
    /// 세션이 닫혔거나 pending 등록에 실패한 요청의 송신 결과를 일관된 실패 상태로 만듭니다.
    /// </summary>
    /// <typeparam name="TResponse">실패시킬 응답 메시지 형식입니다.</typeparam>
    /// <param name="exception">송신 완료와 응답 완료 양쪽에 전달할 실패 원인입니다.</param>
    /// <returns>송신 작업과 응답 작업이 모두 같은 예외로 실패한 결과입니다.</returns>
    private static ISendResult<TResponse> CreateFailedSendResult<TResponse>(Exception exception)
        where TResponse : IResponseMessage
    {
        return new SendResult<TResponse>(
            Task.FromException(exception),
            Task.FromException<TResponse>(exception));
    }

    /// <summary>
    /// pending 등록이 끝난 요청을 transport로 보내고, 송신 실패 시 등록된 pending 요청도 실패 처리합니다.
    /// </summary>
    /// <typeparam name="TRequest">전송할 요청 메시지 형식입니다.</typeparam>
    /// <param name="state">요청을 보낼 현재 장치 연결 상태입니다.</param>
    /// <param name="request">이미 pending 목록에 등록된 요청 메시지입니다.</param>
    /// <returns>transport 송신이 끝나면 완료되는 작업입니다.</returns>
    private async Task SendRegisteredRequestAsync<TRequest>(
        DeviceConnectionState state,
        TRequest request)
        where TRequest : IRequestMessage
    {
        try
        {
            // pending 등록이 끝난 요청만 실제 transport로 보냅니다.
            await SendMessageAsync(state, request, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // 송신 실패 뒤 pending을 그대로 두면 호출자가 응답을 계속 기다릴 수 있으므로 즉시 실패 처리합니다.
            state.Session.TryFailPendingResponse(request.CorrelationId, exception);
            throw;
        }
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

                // 큐가 가득 차면 writer가 대기하므로 추가 transport 수신도 함께 backpressure를 받습니다.
                await WriteInboundEnvelopeAsync(
                        deviceId,
                        state,
                        inboundWriter,
                        new InboundEnvelope(message, null),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            var wrapped = new DeviceConnectionException(deviceId, "receive", exception);
            state.ReceivePumpFailure = wrapped;
            state.Session.FailPendingResponses(wrapped);
            _eventSink.OnOperationFailed(deviceId, "receive", wrapped);
            await TryCloseTransportAsync(state.Transport).ConfigureAwait(false);

            // 대기 중인 ReceiveAsync 호출이 있다면 동일한 실패를 즉시 관측할 수 있게 합니다.
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

    private async Task WriteInboundEnvelopeAsync(
        string deviceId,
        DeviceConnectionState state,
        ChannelWriter<InboundEnvelope> inboundWriter,
        InboundEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (inboundWriter.TryWrite(envelope))
        {
            return;
        }

        SignalInboundBackpressure(deviceId, state);

        try
        {
            // queue가 꽉 찬 동안에는 receive pump가 여기서 대기하고,
            // 그만큼 추가 transport 수신도 자연스럽게 backpressure를 받습니다.
            await inboundWriter.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            state.InboundBackpressureSignaled = false;
        }
    }

    private void SignalInboundBackpressure(string deviceId, DeviceConnectionState state)
    {
        if (state.InboundBackpressureSignaled)
        {
            return;
        }

        state.InboundBackpressureSignaled = true;
        _eventSink.OnInboundBackpressure(deviceId, _inboundQueueCapacity);
    }

    private sealed record InboundEnvelope(IMessage? Message, Exception? Exception);

    /// <summary>
    /// 외부 호출자에게 노출되는 장치 세션 facade입니다.
    /// </summary>
    /// <remarks>
    /// 실제 pending 응답 상태와 transport 송신은 <see cref="ConnectionManager"/>가 계속 소유합니다.
    /// 이 facade는 내부 lifecycle 메서드나 queue를 숨기면서 기존 <see cref="IDeviceSession"/> 송신 사용성을 유지합니다.
    /// </remarks>
    private sealed class ConnectedDeviceSession : IDeviceSession
    {
        private readonly ConnectionManager _owner;
        private readonly DeviceConnectionState _state;

        /// <summary>
        /// <see cref="ConnectedDeviceSession"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="owner">실제 송신과 pending 관리를 수행할 connection manager입니다.</param>
        /// <param name="state">이 facade가 대표하는 장치 연결 상태입니다.</param>
        public ConnectedDeviceSession(ConnectionManager owner, DeviceConnectionState state)
        {
            _owner = owner;
            _state = state;
        }

        /// <summary>
        /// facade가 대표하는 장치 식별자를 가져옵니다.
        /// </summary>
        public string DeviceId => _state.Session.DeviceId;

        /// <summary>
        /// 응답을 기다리지 않는 메시지를 manager의 직접 송신 경로로 전달합니다.
        /// </summary>
        /// <param name="message">전송할 메시지입니다.</param>
        /// <returns>transport 송신이 끝나면 완료되는 송신 결과입니다.</returns>
        public ISendResult Send(IMessage message)
        {
            return new SendResult(_owner.SendMessageAsync(_state, message, CancellationToken.None));
        }

        /// <summary>
        /// 요청 메시지를 pending 응답 추적과 실제 transport 송신이 결합된 manager 경로로 전달합니다.
        /// </summary>
        /// <typeparam name="TRequest">전송할 요청 메시지 형식입니다.</typeparam>
        /// <typeparam name="TResponse">기다릴 응답 메시지 형식입니다.</typeparam>
        /// <param name="request">전송할 요청 메시지입니다.</param>
        /// <param name="timeout">요청별 응답 timeout입니다.</param>
        /// <returns>송신 완료와 응답 완료를 각각 관찰할 수 있는 결과입니다.</returns>
        public ISendResult<TResponse> Send<TRequest, TResponse>(
            TRequest request,
            TimeSpan? timeout = null)
            where TRequest : IRequestMessage
            where TResponse : IResponseMessage
        {
            return _owner.SendRequestFromSession<TRequest, TResponse>(_state, request, timeout);
        }
    }

    private Channel<InboundEnvelope> CreateInboundQueue()
    {
        var options = new BoundedChannelOptions(_inboundQueueCapacity)
        {
            // 메시지를 드롭하지 않고 생산자를 대기시켜 메모리 사용량을 제한합니다.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };

        return Channel.CreateBounded<InboundEnvelope>(options);
    }

    private sealed class DeviceConnectionState
    {
        public DeviceConnectionState(
            DeviceSession session,
            TransportMessageSender sender,
            TransportMessageReceiver receiver,
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

        public DeviceSession Session { get; }

        /// <summary>
        /// 외부 호출자에게 반환되는 공개 세션 facade입니다.
        /// </summary>
        public IDeviceSession PublicSession { get; set; } = default!;

        public TransportMessageSender Sender { get; }

        public TransportMessageReceiver Receiver { get; }

        public ITransport Transport { get; }

        public Channel<InboundEnvelope> InboundQueue { get; }

        public CancellationTokenSource ReceivePumpTokenSource { get; }

        public Task ReceivePumpTask { get; set; } = Task.CompletedTask;

        public volatile Exception? ReceivePumpFailure;

        public bool InboundBackpressureSignaled { get; set; }
    }

    private sealed class DeviceOperationGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int LeaseCount { get; set; }
    }

    private static void DropPendingInbound(Channel<InboundEnvelope> inboundQueue)
    {
        // 이전 연결의 잔여 inbound를 제거해 재연결 후 새 세션으로 섞여 들어오지 않게 합니다.
        inboundQueue.Writer.TryComplete();
        while (inboundQueue.Reader.TryRead(out _))
        {
        }
    }

    private static async Task TryCloseTransportAsync(ITransport? transport)
    {
        if (transport is null)
        {
            return;
        }

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

    /// <summary>
    /// facade가 참조하는 연결 상태가 현재 manager에 등록된 활성 상태인지 확인합니다.
    /// </summary>
    /// <param name="expectedState">facade 생성 시점에 연결되어 있던 상태입니다.</param>
    /// <exception cref="InvalidOperationException">해당 상태가 이미 교체되었거나 제거된 경우 발생합니다.</exception>
    private void ThrowIfConnectionStateInactive(DeviceConnectionState expectedState)
    {
        // facade가 만들어진 뒤 같은 device id가 재연결되면 이전 state로는 더 이상 송신하면 안 됩니다.
        var currentState = GetConnectionState(expectedState.Session.DeviceId);
        if (!ReferenceEquals(currentState, expectedState))
        {
            throw new InvalidOperationException($"Session '{expectedState.Session.DeviceId}' is no longer active.");
        }
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

    private async Task<DeviceOperationGate> AcquireDeviceGateAsync(string deviceId, CancellationToken cancellationToken)
    {
        var deviceGate = GetOrCreateDeviceGate(deviceId);

        try
        {
            await deviceGate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            ReleaseDeviceGateLease(deviceId, deviceGate);
            throw;
        }

        return deviceGate;
    }

    private DeviceOperationGate GetOrCreateDeviceGate(string deviceId)
    {
        lock (_syncRoot)
        {
            if (!_deviceOperationGates.TryGetValue(deviceId, out var deviceGate))
            {
                deviceGate = new DeviceOperationGate();
                _deviceOperationGates[deviceId] = deviceGate;
            }

            deviceGate.LeaseCount++;
            return deviceGate;
        }
    }

    private void ReleaseDeviceGate(string deviceId, DeviceOperationGate deviceGate)
    {
        deviceGate.Semaphore.Release();
        ReleaseDeviceGateLease(deviceId, deviceGate);
    }

    private void ReleaseDeviceGateLease(string deviceId, DeviceOperationGate deviceGate)
    {
        var disposeGate = false;

        lock (_syncRoot)
        {
            deviceGate.LeaseCount--;
            if (deviceGate.LeaseCount == 0 &&
                !_connections.ContainsKey(deviceId) &&
                _deviceOperationGates.TryGetValue(deviceId, out var currentGate) &&
                ReferenceEquals(currentGate, deviceGate))
            {
                _deviceOperationGates.Remove(deviceId);
                disposeGate = true;
            }
        }

        if (disposeGate)
        {
            deviceGate.Semaphore.Dispose();
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
                await TryCloseTransportAsync(transport).ConfigureAwait(false);
                throw;
            }
            catch (Exception exception)
            {
                await TryCloseTransportAsync(transport).ConfigureAwait(false);

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
        return type.Equals(ReconnectTypes.None, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLinearPolicy(string type)
    {
        return type.Equals(ReconnectTypes.Linear, StringComparison.OrdinalIgnoreCase);
    }
}
