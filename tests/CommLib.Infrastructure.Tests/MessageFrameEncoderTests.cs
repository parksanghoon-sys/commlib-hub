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
    /// <summary>
    /// Encode_Message_ComposesSerializerAndProtocol 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// Encode_WithDefaultComponents_ReturnsLengthPrefixedMessageIdPayload 작업을 수행합니다.
    /// </summary>
    public void Encode_WithDefaultComponents_ReturnsLengthPrefixedMessageIdPayload()
    {
        var encoder = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol());

        var frame = encoder.Encode(new FakeMessage(12));

        Assert.Equal(
            new byte[]
            {
                0x00, 0x00, 0x00, 0x0B,
                (byte)'m', (byte)'e', (byte)'s', (byte)'s', (byte)'a', (byte)'g', (byte)'e', (byte)'|', (byte)'1', (byte)'2', (byte)'|'
            },
            frame);
    }

    /// <summary>
    /// FakeMessage 작업을 수행합니다.
    /// </summary>
    private sealed record FakeMessage(ushort MessageId) : IMessage;

    /// <summary>
    /// ISerializer 값을 가져옵니다.
    /// </summary>
    private sealed class FakeSerializer : ISerializer
    {
        /// <summary>
        /// _payload 값을 나타냅니다.
        /// </summary>
        private readonly byte[] _payload;

        /// <summary>
        /// <see cref="FakeSerializer"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        public FakeSerializer(byte[] payload)
        {
            _payload = payload;
        }

        /// <summary>
        /// LastMessage 값을 가져오거나 설정합니다.
        /// </summary>
        public IMessage? LastMessage { get; private set; }

        /// <summary>
        /// Serialize 작업을 수행합니다.
        /// </summary>
        public byte[] Serialize(IMessage message)
        {
            LastMessage = message;
            return _payload;
        }

        /// <summary>
        /// Deserialize 작업을 수행합니다.
        /// </summary>
        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// IProtocol 값을 가져옵니다.
    /// </summary>
    private sealed class FakeProtocol : IProtocol
    {
        /// <summary>
        /// _frame 값을 나타냅니다.
        /// </summary>
        private readonly byte[] _frame;

        /// <summary>
        /// <see cref="FakeProtocol"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        public FakeProtocol(byte[] frame)
        {
            _frame = frame;
        }

        /// <summary>
        /// Name 값을 가져오거나 설정합니다.
        /// </summary>
        public string Name => "Fake";

        /// <summary>
        /// LastPayload 값을 가져오거나 설정합니다.
        /// </summary>
        public byte[]? LastPayload { get; private set; }

        /// <summary>
        /// Encode 작업을 수행합니다.
        /// </summary>
        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            LastPayload = payload.ToArray();
            return _frame;
        }

        /// <summary>
        /// TryDecode 작업을 수행합니다.
        /// </summary>
        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            payload = Array.Empty<byte>();
            bytesConsumed = 0;
            return false;
        }
    }
}
