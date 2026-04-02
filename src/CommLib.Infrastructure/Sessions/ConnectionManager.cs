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
    private readonly ITransportFactory _transportFactory;
    private readonly IProtocolFactory _protocolFactory;
    private readonly ISerializerFactory _serializerFactory;
    private readonly Dictionary<string, IDeviceSession> _sessions = new();
    private readonly Dictionary<string, TransportMessageSender> _senders = new();
    private readonly Dictionary<string, TransportMessageReceiver> _receivers = new();
    private readonly Dictionary<string, ITransport> _transports = new();
    private readonly Dictionary<string, Channel<InboundEnvelope>> _inboundQueues = new();
    private readonly Dictionary<string, CancellationTokenSource> _receivePumpTokens = new();
    private readonly Dictionary<string, Task> _receivePumpTasks = new();

    /// <summary>
    /// <see cref="ConnectionManager"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="transportFactory">장치 전송을 생성하는 transport factory입니다.</param>
    /// <param name="protocolFactory">장치 프로토콜을 생성하는 protocol factory입니다.</param>
    /// <param name="serializerFactory">메시지 serializer를 생성하는 serializer factory입니다.</param>
    public ConnectionManager(
        ITransportFactory transportFactory,
        IProtocolFactory protocolFactory,
        ISerializerFactory serializerFactory)
    {
        _transportFactory = transportFactory;
        _protocolFactory = protocolFactory;
        _serializerFactory = serializerFactory;
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
        var transport = _transportFactory.Create(profile.Transport);
        var protocol = _protocolFactory.Create(profile.Protocol);
        var serializer = _serializerFactory.Create(profile.Serializer);
        var sender = new TransportMessageSender(new MessageFrameEncoder(serializer, protocol), transport);
        var decoder = new MessageFrameDecoder(protocol, serializer);
        var receiver = new TransportMessageReceiver(decoder, transport);
        var session = new DeviceSession(profile.DeviceId, profile.RequestResponse);
        var inboundQueue = Channel.CreateUnbounded<InboundEnvelope>();
        var receivePumpTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var receivePumpTask = Task.CompletedTask;

        try
        {
            await transport.OpenAsync(cancellationToken).ConfigureAwait(false);

            if (_sessions.ContainsKey(profile.DeviceId))
            {
                await DisconnectAsync(profile.DeviceId, cancellationToken).ConfigureAwait(false);
            }

            _transports[profile.DeviceId] = transport;
            _senders[profile.DeviceId] = sender;
            _receivers[profile.DeviceId] = receiver;
            _sessions[profile.DeviceId] = session;
            _inboundQueues[profile.DeviceId] = inboundQueue;
            _receivePumpTokens[profile.DeviceId] = receivePumpTokenSource;
            receivePumpTask = RunReceivePumpAsync(
                session,
                receiver,
                inboundQueue.Writer,
                receivePumpTokenSource.Token);
            _receivePumpTasks[profile.DeviceId] = receivePumpTask;
        }
        catch
        {
            receivePumpTokenSource.Cancel();
            receivePumpTokenSource.Dispose();
            DropPendingInbound(inboundQueue);
            await TryCloseTransportAsync(transport).ConfigureAwait(false);
            throw;
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

        if (!_sessions.ContainsKey(deviceId))
        {
            throw new InvalidOperationException($"No session registered for device '{deviceId}'.");
        }

        if (!_transports.TryGetValue(deviceId, out var transport))
        {
            throw new InvalidOperationException($"No transport registered for device '{deviceId}'.");
        }

        await transport.CloseAsync(cancellationToken).ConfigureAwait(false);

        _sessions.Remove(deviceId);
        _senders.Remove(deviceId);
        _receivers.Remove(deviceId);
        _transports.Remove(deviceId);
        StopReceivePump(deviceId);

        return;
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
        if (!_sessions.TryGetValue(deviceId, out var session))
        {
            throw new InvalidOperationException($"No session registered for device '{deviceId}'.");
        }

        if (!_senders.TryGetValue(deviceId, out var sender))
        {
            throw new InvalidOperationException($"No sender registered for device '{deviceId}'.");
        }

        return SendFromSessionAsync(session, sender, message, cancellationToken);
    }

    /// <summary>
    /// 지정한 장치 식별자로 다음 inbound 메시지를 수신합니다.
    /// </summary>
    /// <param name="deviceId">메시지를 수신할 장치 식별자입니다.</param>
    /// <param name="cancellationToken">수신 취소 토큰입니다.</param>
    /// <returns>복원된 inbound 메시지입니다.</returns>
    public async Task<IMessage> ReceiveAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (!_inboundQueues.TryGetValue(deviceId, out var inboundQueue))
        {
            throw new InvalidOperationException($"No session registered for device '{deviceId}'.");
        }

        var envelope = await inboundQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
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
        _sessions.TryGetValue(deviceId, out var session);
        return session;
    }

    /// <summary>
    /// 활성 장치 연결과 수신 수명주기를 모두 정리합니다.
    /// </summary>
    /// <returns>비동기 정리 작업입니다.</returns>
    public async ValueTask DisposeAsync()
    {
        var deviceIds = _sessions.Keys.ToArray();
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

        throw new AggregateException(exceptions);
    }

    private void StopReceivePump(string deviceId)
    {
        if (_receivePumpTokens.Remove(deviceId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        _receivePumpTasks.Remove(deviceId);
        if (_inboundQueues.Remove(deviceId, out var inboundQueue))
        {
            DropPendingInbound(inboundQueue);
        }
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

        if (!_receivers.TryGetValue(deviceId, out var receiver))
        {
            throw new InvalidOperationException($"No receiver registered for device '{deviceId}'.");
        }

        if (!_sessions.TryGetValue(deviceId, out var session))
        {
            throw new InvalidOperationException($"No session registered for device '{deviceId}'.");
        }

        if (!receiver.TryDecode(buffer, out var decodedMessage, out bytesConsumed))
        {
            return false;
        }

        message = decodedMessage;
        if (message is IResponseMessage response)
        {
            session.TryCompleteResponse(response);
        }

        return true;
    }

    private static async Task SendFromSessionAsync(
        IDeviceSession session,
        TransportMessageSender sender,
        IMessage message,
        CancellationToken cancellationToken)
    {
        var result = session.Send(message);
        await result.SendCompletedTask.ConfigureAwait(false);

        if (!session.TryDequeueOutbound(out var outbound) || outbound is null)
        {
            throw new InvalidOperationException($"Session '{session.DeviceId}' did not expose an outbound message.");
        }

        await sender.SendAsync(outbound, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunReceivePumpAsync(
        IDeviceSession session,
        TransportMessageReceiver receiver,
        ChannelWriter<InboundEnvelope> inboundWriter,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await receiver.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (message is IResponseMessage response && session.TryCompleteResponse(response))
                {
                    continue;
                }

                await inboundWriter.WriteAsync(new InboundEnvelope(message, null), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!inboundWriter.TryWrite(new InboundEnvelope(null, exception)))
            {
                inboundWriter.TryComplete(exception);
            }
        }
        finally
        {
            inboundWriter.TryComplete();
        }
    }

    private sealed record InboundEnvelope(IMessage? Message, Exception? Exception);

    private static void DropPendingInbound(Channel<InboundEnvelope> inboundQueue)
    {
        inboundQueue.Writer.TryComplete();
        while (inboundQueue.Reader.TryRead(out _))
        {
        }
    }

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
}
