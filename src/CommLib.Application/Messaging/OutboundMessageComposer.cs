using System.Collections.Generic;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Messaging;

/// <summary>
/// serializer 유형에 따라 outbound 메시지를 구성하는 정적 클래스입니다.
/// </summary>
public static class OutboundMessageComposer
{
    /// <summary>
    /// serializer 유형에 따라 text 또는 binary outbound 메시지를 생성합니다.
    /// </summary>
    /// <param name="serializerType">사용할 serializer 유형입니다.</param>
    /// <param name="messageId">메시지 식별자입니다.</param>
    /// <param name="body">사용자가 입력한 데이터입니다.</param>
    /// <returns>구성된 메시지 인스턴스입니다.</returns>
    public static IMessage Compose(string serializerType, ushort messageId, string body)
    {
        return serializerType switch
        {
            SerializerTypes.AutoBinary => new MessageModel(messageId, body),
            SerializerTypes.RawHex => new BinaryMessageModel(messageId, HexPayloadParser.Parse(body)),
            _ => throw new NotSupportedException($"Unsupported serializer: {serializerType}")
        };
    }

    /// <summary>
    /// bitfield schema와 named field assignment를 raw binary outbound 메시지로 조합합니다.
    /// </summary>
    /// <param name="messageId">메시지 식별자입니다.</param>
    /// <param name="schema">payload compose에 적용할 bitfield schema입니다.</param>
    /// <param name="fieldValues">schema field에 쓸 값 목록입니다.</param>
    /// <returns>compose된 raw payload를 담은 binary outbound 메시지입니다.</returns>
    public static IMessage Compose(ushort messageId, BitFieldPayloadSchema schema, IEnumerable<BitFieldFieldAssignment> fieldValues)
    {
        return new BinaryMessageModel(messageId, BitFieldPayloadSchemaCodec.Compose(schema, fieldValues));
    }
}
