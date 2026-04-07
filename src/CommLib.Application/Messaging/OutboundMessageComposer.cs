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
}
