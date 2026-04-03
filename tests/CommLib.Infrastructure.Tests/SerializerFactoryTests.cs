using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Protocol;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// serializer factory가 설정 형식에 맞는 구현을 생성하는지 검증합니다.
/// </summary>
public sealed class SerializerFactoryTests
{
    /// <summary>
    /// AutoBinary 설정이면 기본 serializer 구현을 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Create_AutoBinaryOptions_ReturnsNoOpSerializer()
    {
        var factory = new SerializerFactory();

        var serializer = factory.Create(new SerializerOptions { Type = "AutoBinary" });

        Assert.IsType<NoOpSerializer>(serializer);
    }

    /// <summary>
    /// 지원하지 않는 serializer 형식이면 예외를 던지는지 확인합니다.
    /// </summary>
    [Fact]
    public void Create_UnsupportedSerializer_ThrowsNotSupportedException()
    {
        var factory = new SerializerFactory();

        Assert.Throws<NotSupportedException>(() => factory.Create(new SerializerOptions { Type = "Json" }));
    }
}
