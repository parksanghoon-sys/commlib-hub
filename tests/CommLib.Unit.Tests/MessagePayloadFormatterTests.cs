using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 메시지 payload 표시 포맷터가 text와 binary 메시지를 모두 다루는지 검증합니다.
/// </summary>
public sealed class MessagePayloadFormatterTests
{
    [Fact]
    /// <summary>
    /// FormatBody_TextMessage_ReturnsTextBody 작업을 수행합니다.
    /// </summary>
    public void FormatBody_TextMessage_ReturnsTextBody()
    {
        var body = MessagePayloadFormatter.FormatBody(new MessageModel(10, "hello"));

        Assert.Equal("hello", body);
    }

    [Fact]
    /// <summary>
    /// FormatBody_BinaryMessage_ReturnsUppercaseHex 작업을 수행합니다.
    /// </summary>
    public void FormatBody_BinaryMessage_ReturnsUppercaseHex()
    {
        var body = MessagePayloadFormatter.FormatBody(new BinaryMessageModel(10, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));

        Assert.Equal("DE AD BE EF", body);
    }

    [Fact]
    /// <summary>
    /// FormatBody_MessageWithoutPayload_ReturnsEmptyString 작업을 수행합니다.
    /// </summary>
    public void FormatBody_MessageWithoutPayload_ReturnsEmptyString()
    {
        var body = MessagePayloadFormatter.FormatBody(new NoPayloadMessage(10));

        Assert.Equal(string.Empty, body);
    }

    [Fact]
    /// <summary>
    /// TryFormatBitFieldSummary_BigEndianSchemaAtOffset_ReturnsFieldSummary 작업을 수행합니다.
    /// </summary>
    public void TryFormatBitFieldSummary_BigEndianSchemaAtOffset_ReturnsFieldSummary()
    {
        var message = new BinaryMessageModel(10, new byte[] { 0xAA, 0x12, 0x34, 0x7F });
        var schema = new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 4,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "prefix", BitOffset = 0, BitLength = 8 },
                new BitFieldPayloadField
                {
                    Name = "register",
                    BitOffset = 8,
                    BitLength = 16,
                    Endianness = BitFieldEndianness.BigEndian
                },
                new BitFieldPayloadField { Name = "tail", BitOffset = 24, BitLength = 8 }
            }
        };

        var result = MessagePayloadFormatter.TryFormatBitFieldSummary(message, schema, out var summary, out var error);

        Assert.True(result);
        Assert.Equal("prefix=170, register=4660, tail=127", summary);
        Assert.Null(error);
    }

    [Fact]
    /// <summary>
    /// TryFormatBitFieldSummary_PayloadLengthMismatch_ReturnsErrorWithoutThrowing 작업을 수행합니다.
    /// </summary>
    public void TryFormatBitFieldSummary_PayloadLengthMismatch_ReturnsErrorWithoutThrowing()
    {
        var message = new BinaryMessageModel(10, new byte[] { 0x12 });
        var schema = new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 2,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "register", BitOffset = 0, BitLength = 16 }
            }
        };

        var result = MessagePayloadFormatter.TryFormatBitFieldSummary(message, schema, out var summary, out var error);

        Assert.False(result);
        Assert.Null(summary);
        Assert.Contains("does not match", error);
    }

    /// <summary>
    /// NoPayloadMessage 작업을 수행합니다.
    /// </summary>
    private sealed record NoPayloadMessage(ushort MessageId) : IMessage;
}
