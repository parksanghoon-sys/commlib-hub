using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 메시지 프레임 디코더가 protocol과 serializer를 조합하는지 검증합니다.
/// </summary>
public sealed class MessageFrameDecoderTests
{
    /// <summary>
    /// 완전한 프레임을 디코드하면 serializer를 통해 메시지를 복원하는지 확인합니다.
    /// </summary>
    [Fact]
    public void TryDecode_CompleteFrame_ReturnsMessage()
    {
        var decoder = new MessageFrameDecoder(
            new FakeProtocol(payload: new byte[] { 0x10, 0x20 }, bytesConsumed: 6, shouldDecode: true),
            new FakeSerializer(new FakeMessage(7)));

        var decoded = decoder.TryDecode(new byte[] { 0x00, 0x00, 0x00, 0x02, 0x10, 0x20 }, out var message, out var bytesConsumed);

        Assert.True(decoded);
        Assert.NotNull(message);
        Assert.Equal((ushort)7, message.MessageId);
        Assert.Equal(6, bytesConsumed);
    }

    /// <summary>
    /// 완전한 프레임이 아니면 메시지를 복원하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void TryDecode_IncompleteFrame_ReturnsFalse()
    {
        var decoder = new MessageFrameDecoder(
            new FakeProtocol(payload: Array.Empty<byte>(), bytesConsumed: 0, shouldDecode: false),
            new FakeSerializer(new FakeMessage(7)));

        var decoded = decoder.TryDecode(new byte[] { 0x00, 0x00 }, out var message, out var bytesConsumed);

        Assert.False(decoded);
        Assert.Null(message);
        Assert.Equal(0, bytesConsumed);
    }

    /// <summary>
    /// 기본 serializer와 protocol을 함께 사용해 실제 메시지를 복원하는지 확인합니다.
    /// </summary>
    [Fact]
    public void TryDecode_WithDefaultComponents_ReturnsMessage()
    {
        var decoder = new MessageFrameDecoder(new LengthPrefixedProtocol(), new NoOpSerializer());

        var decoded = decoder.TryDecode(new byte[] { 0x00, 0x00, 0x00, 0x02, (byte)'1', (byte)'2' }, out var message, out var bytesConsumed);

        Assert.True(decoded);
        Assert.NotNull(message);
        Assert.Equal((ushort)12, message.MessageId);
        Assert.Equal(6, bytesConsumed);
    }

    private sealed record FakeMessage(ushort MessageId) : IMessage;

    private sealed class FakeProtocol : IProtocol
    {
        private readonly byte[] _payload;
        private readonly int _bytesConsumed;
        private readonly bool _shouldDecode;

        public FakeProtocol(byte[] payload, int bytesConsumed, bool shouldDecode)
        {
            _payload = payload;
            _bytesConsumed = bytesConsumed;
            _shouldDecode = shouldDecode;
        }

        public string Name => "Fake";

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
        }

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            payload = _payload;
            bytesConsumed = _bytesConsumed;
            return _shouldDecode;
        }
    }

    private sealed class FakeSerializer : ISerializer
    {
        private readonly IMessage _message;

        public FakeSerializer(IMessage message)
        {
            _message = message;
        }

        public byte[] Serialize(IMessage message)
        {
            throw new NotSupportedException();
        }

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            return _message;
        }
    }
}
