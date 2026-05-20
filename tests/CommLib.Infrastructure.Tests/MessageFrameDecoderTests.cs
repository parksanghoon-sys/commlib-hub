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

        var decoded = decoder.TryDecode(
            new byte[]
            {
                0x00, 0x00, 0x00, 0x0B,
                (byte)'m', (byte)'e', (byte)'s', (byte)'s', (byte)'a', (byte)'g', (byte)'e', (byte)'|', (byte)'1', (byte)'2', (byte)'|'
            },
            out var message,
            out var bytesConsumed);

        Assert.True(decoded);
        Assert.NotNull(message);
        Assert.Equal((ushort)12, message.MessageId);
        Assert.Equal(15, bytesConsumed);
    }

    [Fact]
    public void TryDecodeMemory_WithZeroCopyProtocol_UsesMemoryDecodePath()
    {
        var protocol = new FakeZeroCopyProtocol(payloadOffset: 2, payloadLength: 3, bytesConsumed: 6);
        var serializer = new CapturingSerializer(new FakeMessage(7));
        var decoder = new MessageFrameDecoder(protocol, serializer);
        var frame = new byte[] { 0xAA, 0xBB, 0x10, 0x20, 0x30, 0xCC };

        var decoded = decoder.TryDecode(frame.AsMemory(), out var message, out var bytesConsumed);

        Assert.True(decoded);
        Assert.NotNull(message);
        Assert.Equal((ushort)7, message.MessageId);
        Assert.Equal(6, bytesConsumed);
        Assert.True(protocol.MemoryDecodeCalled);
        Assert.False(protocol.LegacyDecodeCalled);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, serializer.LastPayload);
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

    private sealed class FakeZeroCopyProtocol : IZeroCopyProtocol
    {
        private readonly int _payloadOffset;
        private readonly int _payloadLength;
        private readonly int _bytesConsumed;

        public FakeZeroCopyProtocol(int payloadOffset, int payloadLength, int bytesConsumed)
        {
            _payloadOffset = payloadOffset;
            _payloadLength = payloadLength;
            _bytesConsumed = bytesConsumed;
        }

        public string Name => "ZeroCopyFake";
        public bool MemoryDecodeCalled { get; private set; }
        public bool LegacyDecodeCalled { get; private set; }

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
        }

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            LegacyDecodeCalled = true;
            payload = Array.Empty<byte>();
            bytesConsumed = 0;
            return false;
        }

        public bool TryDecode(ReadOnlyMemory<byte> buffer, out ProtocolDecodeResult result)
        {
            MemoryDecodeCalled = true;
            result = new ProtocolDecodeResult(buffer.Slice(_payloadOffset, _payloadLength), _bytesConsumed);
            return true;
        }
    }

    private sealed class CapturingSerializer : ISerializer
    {
        private readonly IMessage _message;

        public CapturingSerializer(IMessage message)
        {
            _message = message;
        }

        public byte[] LastPayload { get; private set; } = Array.Empty<byte>();

        public byte[] Serialize(IMessage message)
        {
            throw new NotSupportedException();
        }

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            LastPayload = payload.ToArray();
            return _message;
        }
    }
}
