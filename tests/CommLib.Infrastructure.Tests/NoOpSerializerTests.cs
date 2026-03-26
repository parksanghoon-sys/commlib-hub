using CommLib.Domain.Messaging;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// NoOpSerializer의 최소 직렬화/역직렬화 동작을 검증합니다.
/// </summary>
public sealed class NoOpSerializerTests
{
    /// <summary>
    /// 메시지 식별자를 UTF-8 숫자 문자열로 직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Serialize_Message_ReturnsUtf8MessageIdBytes()
    {
        var serializer = new NoOpSerializer();

        var payload = serializer.Serialize(new FakeMessage(12));

        Assert.Equal(new byte[] { (byte)'1', (byte)'2' }, payload);
    }

    /// <summary>
    /// UTF-8 숫자 문자열을 메시지로 역직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Deserialize_ValidPayload_ReturnsMessage()
    {
        var serializer = new NoOpSerializer();

        var message = serializer.Deserialize(new byte[] { (byte)'4', (byte)'2' });

        Assert.Equal((ushort)42, message.MessageId);
    }

    /// <summary>
    /// 숫자가 아닌 payload는 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Deserialize_InvalidPayload_Throws()
    {
        var serializer = new NoOpSerializer();

        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(new byte[] { (byte)'A' }));
    }

    private sealed record FakeMessage(ushort MessageId) : IMessage;
}
