using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Transport;

namespace CommLib.Infrastructure.Factories;

/// <summary>
/// Creates transport stubs based on transport option types.
/// </summary>
public sealed class TransportFactory : ITransportFactory
{
    /// <summary>
    /// Creates a transport implementation matching the supplied transport options.
    /// </summary>
    /// <param name="options">The transport options describing the desired transport.</param>
    /// <returns>A transport implementation appropriate for the supplied options.</returns>
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
