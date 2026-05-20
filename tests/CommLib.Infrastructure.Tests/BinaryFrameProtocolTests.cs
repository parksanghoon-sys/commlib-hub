using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 설정 기반 binary frame protocol의 start bytes, length prefix, checksum 동작을 검증합니다.
/// </summary>
public sealed class BinaryFrameProtocolTests
{
    [Fact]
    public void Encode_WithStartLengthAndCrc16Modbus_ReturnsConfiguredFrame()
    {
        var protocol = CreateProtocol();
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        var frame = protocol.Encode(payload);

        Assert.Equal(
            new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x10, 0x20, 0x30, 0x05, 0x5A },
            frame);
    }

    [Fact]
    public void TryDecode_WithStartLengthAndCrc16Modbus_ReturnsPayloadAndConsumedLength()
    {
        var protocol = CreateProtocol();
        var frame = new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x10, 0x20, 0x30, 0x05, 0x5A, 0x99 };

        var decoded = protocol.TryDecode(frame, out var payload, out var consumed);

        Assert.True(decoded);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, payload);
        Assert.Equal(9, consumed);
    }

    [Fact]
    public void TryDecode_IncompleteFrame_ReturnsFalse()
    {
        var protocol = CreateProtocol();
        var frame = new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x10 };

        var decoded = protocol.TryDecode(frame, out var payload, out var consumed);

        Assert.False(decoded);
        Assert.Empty(payload);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void TryDecode_WhenStartBytesMismatch_Throws()
    {
        var protocol = CreateProtocol();
        var frame = new byte[] { 0xAB, 0x55, 0x00, 0x03, 0x10, 0x20, 0x30, 0x05, 0x5A };

        var exception = Assert.Throws<InvalidOperationException>(() => protocol.TryDecode(frame, out _, out _));

        Assert.Equal("Frame start bytes do not match the configured BinaryFrame start bytes.", exception.Message);
    }

    [Fact]
    public void TryDecode_WhenChecksumMismatch_Throws()
    {
        var protocol = CreateProtocol();
        var frame = new byte[] { 0xAA, 0x55, 0x00, 0x03, 0x10, 0x20, 0x31, 0x05, 0x5A };

        var exception = Assert.Throws<InvalidOperationException>(() => protocol.TryDecode(frame, out _, out _));

        Assert.Equal("Frame checksum is invalid.", exception.Message);
    }

    private static BinaryFrameProtocol CreateProtocol()
    {
        return new BinaryFrameProtocol(
            new BinaryFrameOptions
            {
                StartHex = "AA 55",
                LengthPrefix = new BinaryFrameLengthPrefixOptions
                {
                    SizeBytes = 2,
                    Endianness = BitFieldEndianness.BigEndian
                },
                Checksum = new BinaryFrameChecksumOptions
                {
                    Type = BinaryFrameChecksumTypes.Crc16Modbus,
                    Endianness = BitFieldEndianness.LittleEndian,
                    Coverage = BinaryFrameChecksumCoverageTypes.FrameWithoutChecksum
                }
            },
            maxFrameLength: 64);
    }
}
