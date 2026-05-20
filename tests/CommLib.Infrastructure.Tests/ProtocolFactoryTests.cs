using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 프로토콜 팩터리가 기대한 구현을 생성하는지 검증합니다.
/// </summary>
public sealed class ProtocolFactoryTests
{
    [Fact]
    public void Create_LengthPrefixedOptions_ReturnsLengthPrefixedProtocol()
    {
        var factory = new ProtocolFactory();

        var protocol = factory.Create(new ProtocolOptions
        {
            Type = "LengthPrefixed",
            MaxFrameLength = 4096
        });

        var lengthPrefixed = Assert.IsType<LengthPrefixedProtocol>(protocol);
        Assert.Equal(4096, lengthPrefixed.MaxFrameLength);
    }

    [Fact]
    public void Create_LowercaseLengthPrefixedType_ReturnsLengthPrefixedProtocol()
    {
        var factory = new ProtocolFactory();

        var protocol = factory.Create(new ProtocolOptions { Type = "lengthprefixed" });

        Assert.IsType<LengthPrefixedProtocol>(protocol);
    }

    [Fact]
    public void Create_BinaryFrameOptions_ReturnsBinaryFrameProtocol()
    {
        var factory = new ProtocolFactory();

        var protocol = factory.Create(new ProtocolOptions
        {
            Type = ProtocolTypes.BinaryFrame,
            MaxFrameLength = 64,
            BinaryFrame = new BinaryFrameOptions
            {
                StartHex = "AA 55",
                LengthPrefix = new BinaryFrameLengthPrefixOptions
                {
                    SizeBytes = 1
                }
            }
        });

        var binaryFrame = Assert.IsType<BinaryFrameProtocol>(protocol);
        var frame = binaryFrame.Encode(new byte[] { 0x7F });

        Assert.True(binaryFrame.TryDecode(frame, out var payload, out var consumed));
        Assert.Equal(new byte[] { 0x7F }, payload);
        Assert.Equal(frame.Length, consumed);
    }

    [Fact]
    public void Create_UnsupportedProtocol_ThrowsNotSupportedException()
    {
        var factory = new ProtocolFactory();

        Assert.Throws<NotSupportedException>(() => factory.Create(new ProtocolOptions { Type = "Custom" }));
    }
}
