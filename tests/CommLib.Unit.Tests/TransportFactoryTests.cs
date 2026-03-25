using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 지원하는 전송 옵션 형식에 대한 전송 팩토리 동작을 검증합니다.
/// </summary>
public sealed class TransportFactoryTests
{
    /// <summary>
    /// TCP 클라이언트 옵션이 TCP 전송 인스턴스를 생성하는지 확인합니다.
    /// </summary>
    [Fact]
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
    /// 지원하지 않는 전송 옵션은 거부되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Create_UnsupportedOptions_ThrowsNotSupportedException()
    {
        var factory = new TransportFactory();

        Assert.Throws<NotSupportedException>(() => factory.Create(new UnsupportedTransportOptions
        {
            Type = "Unsupported"
        }));
    }

    /// <summary>
    /// 테스트 전용 미지원 전송 옵션 형식입니다.
    /// </summary>
    private sealed record UnsupportedTransportOptions : TransportOptions;
}
