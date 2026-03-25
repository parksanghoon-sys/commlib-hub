using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

public sealed class TcpTransport : ITransport
{
    public string Name => "TcpTransport";
}

public sealed class UdpTransport : ITransport
{
    public string Name => "UdpTransport";
}

public sealed class SerialTransport : ITransport
{
    public string Name => "SerialTransport";
}

public sealed class MulticastTransport : ITransport
{
    public string Name => "MulticastTransport";
}
