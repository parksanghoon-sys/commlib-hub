using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Transport;

namespace CommLib.Infrastructure.Factories;

public sealed class TransportFactory : ITransportFactory
{
    public ITransport Create(TransportOptions options)
    {
        return options switch
        {
            TcpClientTransportOptions => new TcpTransport(),
            UdpTransportOptions => new UdpTransport(),
            SerialTransportOptions => new SerialTransport(),
            MulticastTransportOptions => new MulticastTransport(),
            _ => throw new NotSupportedException($"Unsupported transport: {options.GetType().Name}")
        };
    }
}
