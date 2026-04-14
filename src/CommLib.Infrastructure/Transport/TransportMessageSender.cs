using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Protocol;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// 메시지를 프레임으로 인코드한 뒤 전송 매체로 전달하는 송신기입니다.
/// </summary>
public sealed class TransportMessageSender
{
    /// <summary>
    /// _frameEncoder 값을 나타냅니다.
    /// </summary>
    private readonly MessageFrameEncoder _frameEncoder;
    /// <summary>
    /// _transport 값을 나타냅니다.
    /// </summary>
    private readonly ITransport _transport;

    /// <summary>
    /// <see cref="TransportMessageSender"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="frameEncoder">메시지 프레임 인코더입니다.</param>
    /// <param name="transport">프레임을 내보낼 전송 매체입니다.</param>
    public TransportMessageSender(MessageFrameEncoder frameEncoder, ITransport transport)
    {
        _frameEncoder = frameEncoder;
        _transport = transport;
    }

    /// <summary>
    /// 메시지를 프레임으로 인코드한 뒤 전송합니다.
    /// </summary>
    /// <param name="message">전송할 메시지입니다.</param>
    /// <param name="cancellationToken">전송 취소 토큰입니다.</param>
    /// <returns>전송 작업입니다.</returns>
    public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        var frame = _frameEncoder.Encode(message);
        return _transport.SendAsync(frame, cancellationToken);
    }
}
