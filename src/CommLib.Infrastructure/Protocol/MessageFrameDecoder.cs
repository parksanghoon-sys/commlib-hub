using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 입력 프레임에서 메시지를 복원하는 디코더입니다.
/// </summary>
public sealed class MessageFrameDecoder
{
    /// <summary>
    /// _protocol 값을 나타냅니다.
    /// </summary>
    private readonly IProtocol _protocol;
    /// <summary>
    /// _serializer 값을 나타냅니다.
    /// </summary>
    private readonly ISerializer _serializer;

    /// <summary>
    /// <see cref="MessageFrameDecoder"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="protocol">프레임에서 페이로드를 추출하는 프로토콜입니다.</param>
    /// <param name="serializer">페이로드를 메시지로 복원하는 serializer입니다.</param>
    public MessageFrameDecoder(IProtocol protocol, ISerializer serializer)
    {
        _protocol = protocol;
        _serializer = serializer;
    }

    /// <summary>
    /// 입력 버퍼에서 하나의 완전한 메시지를 추출합니다.
    /// </summary>
    /// <param name="buffer">디코드할 입력 버퍼입니다.</param>
    /// <param name="message">복원된 메시지입니다.</param>
    /// <param name="bytesConsumed">소비한 전체 바이트 수입니다.</param>
    /// <returns>완전한 메시지를 복원했으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    public bool TryDecode(ReadOnlySpan<byte> buffer, out IMessage? message, out int bytesConsumed)
    {
        message = null;
        if (!_protocol.TryDecode(buffer, out var payload, out bytesConsumed))
        {
            return false;
        }

        message = _serializer.Deserialize(payload);
        return true;
    }
}
