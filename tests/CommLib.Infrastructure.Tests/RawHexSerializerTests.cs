using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// RawHex serializer가 binary payload와 hex text bridge를 올바르게 처리하는지 검증합니다.
/// </summary>
public sealed class RawHexSerializerTests
{
    [Fact]
    /// <summary>
    /// Serialize_BinaryMessage_AppendsRawPayloadAfterHeader 작업을 수행합니다.
    /// </summary>
    public void Serialize_BinaryMessage_AppendsRawPayloadAfterHeader()
    {
        var serializer = new RawHexSerializer();
        var message = new BinaryMessageModel(12, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

        var payload = serializer.Serialize(message);

        Assert.Equal(
            Encoding.ASCII.GetBytes("message|12|").Concat(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }).ToArray(),
            payload);
    }

    [Fact]
    /// <summary>
    /// Serialize_TextBodyHex_ParsesHexAndAppendsRawPayload 작업을 수행합니다.
    /// </summary>
    public void Serialize_TextBodyHex_ParsesHexAndAppendsRawPayload()
    {
        var serializer = new RawHexSerializer();

        var payload = serializer.Serialize(new MessageModel(12, "DE AD be ef"));

        Assert.Equal(
            Encoding.ASCII.GetBytes("message|12|").Concat(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }).ToArray(),
            payload);
    }

    [Fact]
    /// <summary>
    /// Deserialize_MessagePayload_ReturnsBinaryMessage 작업을 수행합니다.
    /// </summary>
    public void Deserialize_MessagePayload_ReturnsBinaryMessage()
    {
        var serializer = new RawHexSerializer();
        var payload = Encoding.ASCII.GetBytes("message|12|").Concat(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }).ToArray();

        var message = Assert.IsType<BinaryMessageModel>(serializer.Deserialize(payload));

        Assert.Equal((ushort)12, message.MessageId);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, message.Payload.ToArray());
    }

    [Fact]
    /// <summary>
    /// Deserialize_ResponsePayload_ReturnsBinaryResponseMessage 작업을 수행합니다.
    /// </summary>
    public void Deserialize_ResponsePayload_ReturnsBinaryResponseMessage()
    {
        var serializer = new RawHexSerializer();
        var correlationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var payload = Encoding.ASCII
            .GetBytes($"response|9|{correlationId:D}|1|")
            .Concat(new byte[] { 0x01, 0x02, 0x03 })
            .ToArray();

        var message = Assert.IsType<BinaryResponseMessageModel>(serializer.Deserialize(payload));

        Assert.Equal((ushort)9, message.MessageId);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.True(message.IsSuccess);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, message.Payload.ToArray());
    }

    [Fact]
    /// <summary>
    /// Serialize_InvalidHexBody_Throws 작업을 수행합니다.
    /// </summary>
    public void Serialize_InvalidHexBody_Throws()
    {
        var serializer = new RawHexSerializer();

        Assert.Throws<InvalidOperationException>(() => serializer.Serialize(new MessageModel(12, "GG")));
    }
}
