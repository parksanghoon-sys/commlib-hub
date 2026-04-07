using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 메시지 payload 표시 포맷터가 text와 binary 메시지를 모두 다루는지 검증합니다.
/// </summary>
public sealed class MessagePayloadFormatterTests
{
    [Fact]
    public void FormatBody_TextMessage_ReturnsTextBody()
    {
        var body = MessagePayloadFormatter.FormatBody(new MessageModel(10, "hello"));

        Assert.Equal("hello", body);
    }

    [Fact]
    public void FormatBody_BinaryMessage_ReturnsUppercaseHex()
    {
        var body = MessagePayloadFormatter.FormatBody(new BinaryMessageModel(10, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));

        Assert.Equal("DE AD BE EF", body);
    }

    [Fact]
    public void FormatBody_MessageWithoutPayload_ReturnsEmptyString()
    {
        var body = MessagePayloadFormatter.FormatBody(new NoPayloadMessage(10));

        Assert.Equal(string.Empty, body);
    }

    private sealed record NoPayloadMessage(ushort MessageId) : IMessage;
}
