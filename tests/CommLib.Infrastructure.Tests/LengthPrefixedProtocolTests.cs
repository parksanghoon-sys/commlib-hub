using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 길이 prefix 프로토콜의 프레이밍 동작을 검증합니다.
/// </summary>
public sealed class LengthPrefixedProtocolTests
{
    /// <summary>
    /// 페이로드를 인코드하면 4바이트 길이 prefix와 본문이 함께 포함되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Encode_Payload_ReturnsBigEndianLengthPrefixedFrame()
    {
        var protocol = new LengthPrefixedProtocol();
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        var frame = protocol.Encode(payload);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x10, 0x20, 0x30 }, frame);
    }

    /// <summary>
    /// 완전한 프레임을 디코드하면 원본 페이로드와 소비 바이트 수를 반환하는지 확인합니다.
    /// </summary>
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

    /// <summary>
    /// 버퍼에 후속 데이터가 있어도 첫 프레임만 읽고 소비 길이를 정확히 반환하는지 확인합니다.
    /// </summary>
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

    /// <summary>
    /// 헤더만 있고 본문이 모자라면 아직 프레임을 완성하지 않은 것으로 처리하는지 확인합니다.
    /// </summary>
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

    /// <summary>
    /// 음수 길이 prefix는 잘못된 프레임으로 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void TryDecode_NegativeLength_Throws()
    {
        var protocol = new LengthPrefixedProtocol();
        var frame = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Throws<InvalidOperationException>(() => protocol.TryDecode(frame, out _, out _));
    }
}
