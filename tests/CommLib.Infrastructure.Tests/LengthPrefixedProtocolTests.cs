using System.Runtime.InteropServices;
using CommLib.Domain.Protocol;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 길이 prefix 프로토콜의 프레이밍 동작을 검증합니다.
/// </summary>
public sealed class LengthPrefixedProtocolTests
{
    [Fact]
    public void Constructor_MaxFrameLengthSmallerThanHeader_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LengthPrefixedProtocol(3));
    }

    [Fact]
    public void Encode_Payload_ReturnsBigEndianLengthPrefixedFrame()
    {
        var protocol = new LengthPrefixedProtocol();
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        var frame = protocol.Encode(payload);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 }, frame);
    }

    [Fact]
    public void Encode_FrameLongerThanConfiguredMaximum_Throws()
    {
        var protocol = new LengthPrefixedProtocol(6);
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        var exception = Assert.Throws<InvalidOperationException>(() => protocol.Encode(payload));

        Assert.Equal("Frame length 7 exceeds the configured maximum of 6.", exception.Message);
    }

    [Fact]
    public void FrameWriter_WithPayloadSlot_ReturnsBigEndianLengthPrefixedFrame()
    {
        var protocol = Assert.IsAssignableFrom<IFrameEncodingProtocol>(new LengthPrefixedProtocol());
        var layout = protocol.CreateFrameLayout(payloadLength: 3);
        var frame = new byte[layout.FrameLength];

        protocol.WriteFramePrefix(frame, layout);
        new byte[] { 0x10, 0x20, 0x30 }.CopyTo(frame.AsSpan(layout.PayloadOffset, layout.PayloadLength));
        protocol.WriteFrameSuffix(frame, layout);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 }, frame);
    }

    [Fact]
    public void CreateFrameLayout_FrameLongerThanConfiguredMaximum_Throws()
    {
        var protocol = Assert.IsAssignableFrom<IFrameEncodingProtocol>(new LengthPrefixedProtocol(6));

        var exception = Assert.Throws<InvalidOperationException>(() => protocol.CreateFrameLayout(payloadLength: 3));

        Assert.Equal("Frame length 7 exceeds the configured maximum of 6.", exception.Message);
    }

    [Fact]
    public void TryDecode_CompleteFrame_ReturnsPayloadAndConsumedLength()
    {
        var protocol = new LengthPrefixedProtocol();
        var frame = new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 };

        var decoded = protocol.TryDecode(frame, out var payload, out var consumed);

        Assert.True(decoded);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, payload);
        Assert.Equal(7, consumed);
    }

    [Fact]
    public void TryDecodeMemory_CompleteFrame_ReturnsPayloadSliceWithoutCopy()
    {
        var protocol = Assert.IsAssignableFrom<IZeroCopyProtocol>(new LengthPrefixedProtocol());
        var frame = new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 };

        var decoded = protocol.TryDecode(frame.AsMemory(), out var result);

        Assert.True(decoded);
        Assert.Equal(7, result.BytesConsumed);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, result.Payload.ToArray());
        Assert.True(MemoryMarshal.TryGetArray(result.Payload, out var segment));
        Assert.Same(frame, segment.Array);
        Assert.Equal(4, segment.Offset);
        Assert.Equal(3, segment.Count);
    }

    [Fact]
    public void TryDecode_FrameWithTrailingBytes_ConsumesOnlySingleFrame()
    {
        var protocol = new LengthPrefixedProtocol();
        var frame = new byte[] { 0x00, 0x00, 0x00, 0x02, 0xAA, 0xBB, 0xFF, 0xEE };

        var decoded = protocol.TryDecode(frame, out var payload, out var consumed);

        Assert.True(decoded);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, payload);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void TryDecode_IncompleteFrame_ReturnsFalse()
    {
        var protocol = new LengthPrefixedProtocol();
        var frame = new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20 };

        var decoded = protocol.TryDecode(frame, out var payload, out var consumed);

        Assert.False(decoded);
        Assert.Empty(payload);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void TryDecode_NegativeLength_Throws()
    {
        var protocol = new LengthPrefixedProtocol();
        var frame = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Throws<InvalidOperationException>(() => protocol.TryDecode(frame, out _, out _));
    }

    [Fact]
    public void TryDecode_FrameLongerThanConfiguredMaximum_Throws()
    {
        var protocol = new LengthPrefixedProtocol(6);
        var frame = new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 };

        var exception = Assert.Throws<InvalidOperationException>(() => protocol.TryDecode(frame, out _, out _));

        Assert.Equal("Frame length 7 exceeds the configured maximum of 6.", exception.Message);
    }
}
