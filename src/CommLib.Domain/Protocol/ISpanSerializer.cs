using CommLib.Domain.Messaging;

namespace CommLib.Domain.Protocol;

/// <summary>
/// payload 배열을 별도로 만들지 않고 caller-provided span에 직렬화 결과를 기록하는 선택적 serializer 계약입니다.
/// </summary>
public interface ISpanSerializer : ISerializer
{
    /// <summary>
    /// 지정한 메시지가 직렬화될 때 필요한 payload byte 수를 계산합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>필요한 destination 길이입니다.</returns>
    int GetSerializedLength(IMessage message);

    /// <summary>
    /// 메시지를 caller가 제공한 destination span에 직접 직렬화합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <param name="destination">정확한 길이로 준비된 출력 span입니다.</param>
    void Serialize(IMessage message, Span<byte> destination);
}
