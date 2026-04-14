using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 지원하는 전송 옵션 형식에 대한 전송 팩토리 동작을 검증합니다.
/// </summary>
public sealed class TransportFactoryTests
{
    /// <summary>
    /// TCP 클라이언트 옵션이 TCP 전송 인스턴스를 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Create_TcpOptions_ReturnsTcpTransport 작업을 수행합니다.
    /// </summary>
    public void Create_TcpOptions_ReturnsTcpTransport()
    {
        var factory = new TransportFactory();

        var transport = factory.Create(new TcpClientTransportOptions
        {
            Type = "TcpClient",
            Host = "127.0.0.1",
            Port = 9000
        });

        Assert.IsType<TcpTransport>(transport);
    }

    /// <summary>
    /// UDP 전송 옵션이 UDP 전송 인스턴스를 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Create_UdpOptions_ReturnsUdpTransport 작업을 수행합니다.
    /// </summary>
    public void Create_UdpOptions_ReturnsUdpTransport()
    {
        var factory = new TransportFactory();

        var transport = factory.Create(new UdpTransportOptions
        {
            Type = "Udp",
            LocalPort = 9001,
            RemoteHost = "127.0.0.1",
            RemotePort = 9002
        });

        Assert.IsType<UdpTransport>(transport);
    }

    /// <summary>
    /// 시리얼 전송 옵션이 시리얼 전송 인스턴스를 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Create_SerialOptions_ReturnsSerialTransport 작업을 수행합니다.
    /// </summary>
    public void Create_SerialOptions_ReturnsSerialTransport()
    {
        var factory = new TransportFactory();

        var transport = factory.Create(new SerialTransportOptions
        {
            Type = "Serial",
            PortName = "COM1"
        });

        Assert.IsType<SerialTransport>(transport);
    }

    /// <summary>
    /// 멀티캐스트 전송 옵션이 멀티캐스트 전송 인스턴스를 생성하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Create_MulticastOptions_ReturnsMulticastTransport 작업을 수행합니다.
    /// </summary>
    public void Create_MulticastOptions_ReturnsMulticastTransport()
    {
        var factory = new TransportFactory();

        var transport = factory.Create(new MulticastTransportOptions
        {
            Type = "Multicast",
            GroupAddress = "239.0.0.1",
            Port = 5000
        });

        Assert.IsType<MulticastTransport>(transport);
    }

    /// <summary>
    /// 같은 전송 옵션 형식으로 여러 번 생성해도 매번 새 인스턴스를 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Create_SameTransportTypeTwice_ReturnsDifferentInstances 작업을 수행합니다.
    /// </summary>
    public void Create_SameTransportTypeTwice_ReturnsDifferentInstances()
    {
        var factory = new TransportFactory();
        var options = new TcpClientTransportOptions
        {
            Type = "TcpClient",
            Host = "127.0.0.1",
            Port = 9000
        };

        var first = factory.Create(options);
        var second = factory.Create(options);

        Assert.IsType<TcpTransport>(first);
        Assert.IsType<TcpTransport>(second);
        Assert.NotSame(first, second);
    }

    /// <summary>
    /// 지원하지 않는 전송 옵션은 거부되는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Create_UnsupportedOptions_ThrowsNotSupportedException 작업을 수행합니다.
    /// </summary>
    public void Create_UnsupportedOptions_ThrowsNotSupportedException()
    {
        var factory = new TransportFactory();

        Assert.Throws<NotSupportedException>(() => factory.Create(new UnsupportedTransportOptions
        {
            Type = "Unsupported"
        }));
    }

    /// <summary>
    /// 지원하지 않는 전송 옵션 예외 메시지에 형식 이름이 포함되는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Create_UnsupportedOptions_IncludesTypeNameInExceptionMessage 작업을 수행합니다.
    /// </summary>
    public void Create_UnsupportedOptions_IncludesTypeNameInExceptionMessage()
    {
        var factory = new TransportFactory();

        var exception = Assert.Throws<NotSupportedException>(() => factory.Create(new UnsupportedTransportOptions
        {
            Type = "Unsupported"
        }));

        Assert.Contains(nameof(UnsupportedTransportOptions), exception.Message);
    }

    /// <summary>
    /// 테스트 전용 미지원 전송 옵션 형식입니다.
    /// </summary>
    private sealed record UnsupportedTransportOptions : TransportOptions;
}
