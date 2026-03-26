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

        _senders[profile.DeviceId] = sender;
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
        if (!_senders.TryGetValue(deviceId, out var sender))
        {
            throw new InvalidOperationException($"No sender registered for device '{deviceId}'.");
        }

        return sender.SendAsync(message, cancellationToken);
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
}
