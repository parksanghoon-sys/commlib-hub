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

        Assert.Equal(
            new byte[]
            {
                0x00, 0x00, 0x00, 0x0B,
                (byte)'m', (byte)'e', (byte)'s', (byte)'s', (byte)'a', (byte)'g', (byte)'e', (byte)'|', (byte)'1', (byte)'2', (byte)'|'
            },
            frame);
    }

    [Fact]
    public void Encode_WhenSerializerAndProtocolSupportSpanWriters_WritesPayloadDirectlyIntoFinalFrame()
    {
        var serializer = new SpanFakeSerializer(new byte[] { 0x10, 0x20, 0x30 });
        var protocol = new SpanFakeProtocol();
        var encoder = new MessageFrameEncoder(serializer, protocol);

        var frame = encoder.Encode(new FakeMessage(7));

        Assert.Equal(new byte[] { 0xAA, 0x03, 0x10, 0x20, 0x30, 0x55 }, frame);
        Assert.True(serializer.SpanSerializeCalled);
        Assert.False(serializer.LegacySerializeCalled);
        Assert.True(protocol.FrameWriterCalled);
        Assert.False(protocol.LegacyEncodeCalled);
    }

    /// <summary>
    /// serializer만 span writer를 지원하면 protocol이 최종 frame layout을 제공할 수 없으므로 기존 배열 경로로 되돌아가는지 검증합니다.
    /// </summary>
    [Fact]
    public void Encode_WhenOnlySerializerSupportsSpanWriter_UsesLegacyFallback()
    {
        var serializer = new SpanFakeSerializer(new byte[] { 0x10, 0x20, 0x30 });
        var protocol = new FakeProtocol(new byte[] { 0xAA, 0x10, 0x20, 0x30 });
        var encoder = new MessageFrameEncoder(serializer, protocol);
        var message = new FakeMessage(7);

        var frame = encoder.Encode(message);

        Assert.Equal(new byte[] { 0xAA, 0x10, 0x20, 0x30 }, frame);
        Assert.False(serializer.SpanSerializeCalled);
        Assert.True(serializer.LegacySerializeCalled);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, protocol.LastPayload);
    }

    /// <summary>
    /// protocol만 frame writer를 지원하면 serializer가 payload slot에 직접 쓸 수 없으므로 기존 배열 경로로 되돌아가는지 검증합니다.
    /// </summary>
    [Fact]
    public void Encode_WhenOnlyProtocolSupportsFrameWriter_UsesLegacyFallback()
    {
        var serializer = new FakeSerializer(new byte[] { 0x10, 0x20, 0x30 });
        var protocol = new SpanFakeProtocol();
        var encoder = new MessageFrameEncoder(serializer, protocol);
        var message = new FakeMessage(7);

        var frame = encoder.Encode(message);

        Assert.Same(message, serializer.LastMessage);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, frame);
        Assert.False(protocol.FrameWriterCalled);
        Assert.True(protocol.LegacyEncodeCalled);
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

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
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

    private sealed class SpanFakeSerializer : ISpanSerializer
    {
        private readonly byte[] _payload;

        public SpanFakeSerializer(byte[] payload)
        {
            _payload = payload;
        }

        public bool SpanSerializeCalled { get; private set; }
        public bool LegacySerializeCalled { get; private set; }

        public int GetSerializedLength(IMessage message)
        {
            return _payload.Length;
        }

        public void Serialize(IMessage message, Span<byte> destination)
        {
            SpanSerializeCalled = true;
            _payload.CopyTo(destination);
        }

        public byte[] Serialize(IMessage message)
        {
            LegacySerializeCalled = true;
            return _payload;
        }

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class SpanFakeProtocol : IFrameEncodingProtocol
    {
        public string Name => "SpanFake";
        public bool FrameWriterCalled { get; private set; }
        public bool LegacyEncodeCalled { get; private set; }

        public ProtocolFrameLayout CreateFrameLayout(int payloadLength)
        {
            return new ProtocolFrameLayout(FrameLength: payloadLength + 3, PayloadOffset: 2, PayloadLength: payloadLength);
        }

        public void WriteFramePrefix(Span<byte> frame, ProtocolFrameLayout layout)
        {
            FrameWriterCalled = true;
            frame[0] = 0xAA;
            frame[1] = checked((byte)layout.PayloadLength);
        }

        public void WriteFrameSuffix(Span<byte> frame, ProtocolFrameLayout layout)
        {
            frame[layout.PayloadOffset + layout.PayloadLength] = 0x55;
        }

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            LegacyEncodeCalled = true;
            return payload.ToArray();
        }

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            payload = Array.Empty<byte>();
            bytesConsumed = 0;
            return false;
        }
    }
}
