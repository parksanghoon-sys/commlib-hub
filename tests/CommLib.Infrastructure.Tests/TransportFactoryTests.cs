using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

public sealed class TransportFactoryTests
{
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
}
