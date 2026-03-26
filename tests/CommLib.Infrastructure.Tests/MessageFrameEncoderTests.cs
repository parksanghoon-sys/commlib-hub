using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 메시지 프레임 인코더가 serializer와 protocol을 올바르게 조합하는지 검증합니다.
/// </summary>
public sealed class MessageFrameEncoderTests
{
    /// <summary>
    /// 메시지를 인코드하면 serializer 결과를 protocol 입력으로 전달하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Encode_Message_ComposesSerializerAndProtocol()
    {
        var serializer = new FakeSerializer(new byte[] { 0x10, 0x20, 0x30 });
        var protocol = new FakeProtocol(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 });
        var encoder = new MessageFrameEncoder(serializer, protocol);
        var message = new FakeMessage(7);

        var frame = encoder.Encode(message);

        Assert.Same(message, serializer.LastMessage);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, protocol.LastPayload);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 }, frame);
    }

    /// <summary>
    /// NoOpSerializer와 LengthPrefixedProtocol을 함께 사용하면 실제 프레임을 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Encode_WithDefaultComponents_ReturnsLengthPrefixedMessageIdPayload()
    {
        var encoder = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol());

        var frame = encoder.Encode(new FakeMessage(12));

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x02, (byte)'1', (byte)'2' }, frame);
    }

    private sealed record FakeMessage(ushort MessageId) : IMessage;

    private sealed class FakeSerializer : ISerializer
    {
        private readonly byte[] _payload;

        public FakeSerializer(byte[] payload)
        {
            _payload = payload;
        }

        public IMessage? LastMessage { get; private set; }

        public byte[] Serialize(IMessage message)
        {
            LastMessage = message;
            return _payload;
        }
    }

    private sealed class FakeProtocol : IProtocol
    {
        private readonly byte[] _frame;

        public FakeProtocol(byte[] frame)
        {
            _frame = frame;
        }

        public string Name => "Fake";

        public byte[]? LastPayload { get; private set; }

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            LastPayload = payload.ToArray();
            return _frame;
        }

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            payload = Array.Empty<byte>();
            bytesConsumed = 0;
            return false;
        }
    }
}
