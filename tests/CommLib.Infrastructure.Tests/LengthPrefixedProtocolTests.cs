using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 길이 prefix 프로토콜의 프레이밍 동작을 검증합니다.
/// </summary>
public sealed class LengthPrefixedProtocolTests
{
    [Fact]
    /// <summary>
    /// Constructor_MaxFrameLengthSmallerThanHeader_Throws 작업을 수행합니다.
    /// </summary>
    public void Constructor_MaxFrameLengthSmallerThanHeader_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LengthPrefixedProtocol(3));
    }

    [Fact]
    /// <summary>
    /// Encode_Payload_ReturnsBigEndianLengthPrefixedFrame 작업을 수행합니다.
    /// </summary>
    public void Encode_Payload_ReturnsBigEndianLengthPrefixedFrame()
    {
        var protocol = new LengthPrefixedProtocol();
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        var frame = protocol.Encode(payload);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 }, frame);
    }

    [Fact]
    /// <summary>
    /// Encode_FrameLongerThanConfiguredMaximum_Throws 작업을 수행합니다.
    /// </summary>
    public void Encode_FrameLongerThanConfiguredMaximum_Throws()
    {
        var protocol = new LengthPrefixedProtocol(6);
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        var exception = Assert.Throws<InvalidOperationException>(() => protocol.Encode(payload));

        Assert.Equal("Frame length 7 exceeds the configured maximum of 6.", exception.Message);
    }

    [Fact]
    /// <summary>
    /// TryDecode_CompleteFrame_ReturnsPayloadAndConsumedLength 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// TryDecode_FrameWithTrailingBytes_ConsumesOnlySingleFrame 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// TryDecode_IncompleteFrame_ReturnsFalse 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// TryDecode_NegativeLength_Throws 작업을 수행합니다.
    /// </summary>
    public void TryDecode_NegativeLength_Throws()
    {
        var protocol = new LengthPrefixedProtocol();
        var frame = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Throws<InvalidOperationException>(() => protocol.TryDecode(frame, out _, out _));
    }

    [Fact]
    /// <summary>
    /// TryDecode_FrameLongerThanConfiguredMaximum_Throws 작업을 수행합니다.
    /// </summary>
    public void TryDecode_FrameLongerThanConfiguredMaximum_Throws()
    {
        var protocol = new LengthPrefixedProtocol(6);
        var frame = new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 };

        var exception = Assert.Throws<InvalidOperationException>(() => protocol.TryDecode(frame, out _, out _));

        Assert.Equal("Frame length 7 exceeds the configured maximum of 6.", exception.Message);
    }
}
