using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 메시지를 직렬화한 뒤 프로토콜 프레임으로 감싸는 인코더입니다.
/// </summary>
public sealed class MessageFrameEncoder
{
    /// <summary>
    /// _serializer 값을 나타냅니다.
    /// </summary>
    private readonly ISerializer _serializer;
    /// <summary>
    /// _protocol 값을 나타냅니다.
    /// </summary>
    private readonly IProtocol _protocol;

    /// <summary>
    /// <see cref="MessageFrameEncoder"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="serializer">메시지 페이로드 직렬화기입니다.</param>
    /// <param name="protocol">직렬화된 페이로드를 프레임으로 감싸는 프로토콜입니다.</param>
    public MessageFrameEncoder(ISerializer serializer, IProtocol protocol)
    {
        _serializer = serializer;
        _protocol = protocol;
    }

    /// <summary>
    /// 메시지를 직렬화하고 프로토콜 프레임으로 인코드합니다.
    /// </summary>
    /// <param name="message">인코드할 메시지입니다.</param>
    /// <returns>전송 가능한 프레임 바이트 배열입니다.</returns>
    public byte[] Encode(IMessage message)
    {
        var payload = _serializer.Serialize(message);
        return _protocol.Encode(payload);
    }
}
