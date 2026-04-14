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
    /// <summary>
    /// Create_LengthPrefixedOptions_ReturnsLengthPrefixedProtocol 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// Create_LowercaseLengthPrefixedType_ReturnsLengthPrefixedProtocol 작업을 수행합니다.
    /// </summary>
    public void Create_LowercaseLengthPrefixedType_ReturnsLengthPrefixedProtocol()
    {
        var factory = new ProtocolFactory();

        var protocol = factory.Create(new ProtocolOptions { Type = "lengthprefixed" });

        Assert.IsType<LengthPrefixedProtocol>(protocol);
    }

    [Fact]
    /// <summary>
    /// Create_UnsupportedProtocol_ThrowsNotSupportedException 작업을 수행합니다.
    /// </summary>
    public void Create_UnsupportedProtocol_ThrowsNotSupportedException()
    {
        var factory = new ProtocolFactory();

        Assert.Throws<NotSupportedException>(() => factory.Create(new ProtocolOptions { Type = "Custom" }));
    }
}
