using CommLib.Application.Messaging;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// outbound message composer媛 serializer ?좏깮???곕씪 ?곸젅??硫붿떆吏 紐⑤뜽??留뚮뱶?붿? 寃利앺빀?덈떎.
/// </summary>
public sealed class OutboundMessageComposerTests
{
    [Fact]
    public void Compose_AutoBinary_ReturnsTextMessageModel()
    {
        var message = OutboundMessageComposer.Compose(SerializerTypes.AutoBinary, 10, "hello");

        var typed = Assert.IsType<MessageModel>(message);
        Assert.Equal((ushort)10, typed.MessageId);
        Assert.Equal("hello", typed.Body);
    }

    [Fact]
    public void Compose_RawHex_ReturnsBinaryMessageModel()
    {
        var message = OutboundMessageComposer.Compose(SerializerTypes.RawHex, 11, "DE AD be ef");

        var typed = Assert.IsType<BinaryMessageModel>(message);
        Assert.Equal((ushort)11, typed.MessageId);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, typed.Payload.ToArray());
    }

    [Fact]
    public void Compose_RawHexWithEmptyBody_ReturnsEmptyBinaryMessageModel()
    {
        var message = OutboundMessageComposer.Compose(SerializerTypes.RawHex, 12, "   ");

        var typed = Assert.IsType<BinaryMessageModel>(message);
        Assert.Equal((ushort)12, typed.MessageId);
        Assert.Empty(typed.Payload.ToArray());
    }

    [Fact]
    public void Compose_RawHexWithInvalidBody_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => OutboundMessageComposer.Compose(SerializerTypes.RawHex, 13, "GG"));
    }

    [Fact]
    public void Compose_BitFieldSchema_ReturnsBinaryMessageModel()
    {
        var schema = new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 2,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "mode", BitOffset = 0, BitLength = 3 },
                new BitFieldPayloadField { Name = "delta", BitOffset = 4, BitLength = 12, ScalarKind = BitFieldScalarKind.Signed }
            }
        };

        var message = OutboundMessageComposer.Compose(
            14,
            schema,
            new[]
            {
                new BitFieldFieldAssignment("mode", 5),
                new BitFieldFieldAssignment("delta", -100)
            });

        var typed = Assert.IsType<BinaryMessageModel>(message);
        Assert.Equal((ushort)14, typed.MessageId);
        Assert.Equal(new byte[] { 0xC5, 0xF9 }, typed.Payload.ToArray());
    }

    [Fact]
    public void Compose_UnsupportedSerializer_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => OutboundMessageComposer.Compose("Custom", 15, "hello"));
    }
}
