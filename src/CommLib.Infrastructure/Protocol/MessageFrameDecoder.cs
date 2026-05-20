using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 입력 프레임에서 메시지를 복원하는 디코더입니다.
/// </summary>
public sealed class MessageFrameDecoder
{
    private readonly IProtocol _protocol;
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

    /// <summary>
    /// 기존 byte[] 호출이 span/memory overload 사이에서 모호해지지 않도록 유지하는 호환성 경로입니다.
    /// </summary>
    /// <param name="buffer">decode할 frame byte 배열입니다.</param>
    /// <param name="message">복원된 메시지입니다.</param>
    /// <param name="bytesConsumed">소비한 frame byte 수입니다.</param>
    /// <returns>완성된 메시지를 복원했으면 <see langword="true"/>입니다.</returns>
    public bool TryDecode(byte[] buffer, out IMessage? message, out int bytesConsumed)
    {
        return TryDecode(buffer.AsSpan(), out message, out bytesConsumed);
    }

    /// <summary>
    /// memory 기반 입력에서는 zero-copy protocol fast path를 우선 사용해 payload 배열 복사를 피합니다.
    /// </summary>
    /// <param name="buffer">decode할 frame 후보 memory입니다.</param>
    /// <param name="message">복원된 메시지입니다.</param>
    /// <param name="bytesConsumed">소비한 frame byte 수입니다.</param>
    /// <returns>완성된 메시지를 복원했으면 <see langword="true"/>입니다.</returns>
    public bool TryDecode(ReadOnlyMemory<byte> buffer, out IMessage? message, out int bytesConsumed)
    {
        message = null;
        bytesConsumed = 0;

        if (_protocol is IZeroCopyProtocol zeroCopyProtocol)
        {
            if (!zeroCopyProtocol.TryDecode(buffer, out var result))
            {
                return false;
            }

            message = _serializer.Deserialize(result.Payload.Span);
            bytesConsumed = result.BytesConsumed;
            return true;
        }

        return TryDecode(buffer.Span, out message, out bytesConsumed);
    }
}
