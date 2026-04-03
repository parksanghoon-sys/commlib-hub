using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 프로토콜 factory가 설정 형식에 맞는 구현을 생성하는지 검증합니다.
/// </summary>
public sealed class ProtocolFactoryTests
{
    /// <summary>
    /// LengthPrefixed 설정이면 길이 prefix 프로토콜을 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Create_LengthPrefixedOptions_ReturnsLengthPrefixedProtocol()
    {
        var factory = new ProtocolFactory();

        var protocol = factory.Create(new ProtocolOptions { Type = "LengthPrefixed" });

        Assert.IsType<LengthPrefixedProtocol>(protocol);
    }

    /// <summary>
    /// 지원하지 않는 프로토콜 형식이면 예외를 던지는지 확인합니다.
    /// </summary>
    [Fact]
    public void Create_UnsupportedProtocol_ThrowsNotSupportedException()
    {
        var factory = new ProtocolFactory();

        Assert.Throws<NotSupportedException>(() => factory.Create(new ProtocolOptions { Type = "Custom" }));
    }
}
