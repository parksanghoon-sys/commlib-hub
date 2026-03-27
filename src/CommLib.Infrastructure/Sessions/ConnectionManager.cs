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
public sealed class ConnectionManager : IConnectionManager
{
    private readonly ITransportFactory _transportFactory;
    private readonly IProtocolFactory _protocolFactory;
    private readonly ISerializerFactory _serializerFactory;
    private readonly Dictionary<string, IDeviceSession> _sessions = new();
    private readonly Dictionary<string, TransportMessageSender> _senders = new();
    private readonly Dictionary<string, TransportMessageReceiver> _receivers = new();

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
    public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        var transport = _transportFactory.Create(profile.Transport);
        var protocol = _protocolFactory.Create(profile.Protocol);
        var serializer = _serializerFactory.Create(profile.Serializer);
        var sender = new TransportMessageSender(new MessageFrameEncoder(serializer, protocol), transport);
        var decoder = new MessageFrameDecoder(protocol, serializer);
        var receiver = new TransportMessageReceiver(decoder, transport);

        _senders[profile.DeviceId] = sender;
        _receivers[profile.DeviceId] = receiver;
        _sessions[profile.DeviceId] = new DeviceSession(profile.DeviceId);
        return Task.CompletedTask;
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
        if (!_receivers.TryGetValue(deviceId, out var receiver))
        {
            throw new InvalidOperationException($"No receiver registered for device '{deviceId}'.");
        }

        if (!_sessions.TryGetValue(deviceId, out var session))
        {
            throw new InvalidOperationException($"No session registered for device '{deviceId}'.");
        }

        var message = await receiver.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        if (message is IResponseMessage response)
        {
            session.TryCompleteResponse(response);
        }

        return message;
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
}
