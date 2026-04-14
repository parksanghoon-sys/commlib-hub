using System.Collections.Generic;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Messaging;

/// <summary>
/// ?좏깮??serializer 紐⑤뱶??留욌뒗 outbound 硫붿떆吏 紐⑤뜽??議고빀?⑸땲??
/// </summary>
public static class OutboundMessageComposer
{
    /// <summary>
    /// serializer ?좏삎???곕씪 text ?먮뒗 binary outbound 硫붿떆吏瑜??앹꽦?⑸땲??
    /// </summary>
    /// <param name="serializerType">?쒖꽦 serializer ?앸퀎?먯엯?덈떎.</param>
    /// <param name="messageId">硫붿떆吏 ?앸퀎?먯엯?덈떎.</param>
    /// <param name="body">?ъ슜???낅젰 蹂몃Ц?낅땲??</param>
    /// <returns>?꾩넚??硫붿떆吏 紐⑤뜽?낅땲??</returns>
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
