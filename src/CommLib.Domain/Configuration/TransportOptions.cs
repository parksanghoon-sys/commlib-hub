namespace CommLib.Domain.Configuration;

public abstract record TransportOptions
{
    public required string Type { get; init; }
}

public sealed record TcpClientTransportOptions : TransportOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public int ConnectTimeoutMs { get; init; } = 3000;
    public int BufferSize { get; init; } = 8192;
    public bool NoDelay { get; init; } = true;
}

public sealed record UdpTransportOptions : TransportOptions
{
    public int LocalPort { get; init; }
    public string? RemoteHost { get; init; }
    public int? RemotePort { get; init; }
}

public sealed record SerialTransportOptions : TransportOptions
{
    public string PortName { get; init; } = string.Empty;
    public int BaudRate { get; init; } = 9600;
    public int DataBits { get; init; } = 8;
    public string Parity { get; init; } = "None";
    public string StopBits { get; init; } = "One";
    public bool HalfDuplex { get; init; }
    public int TurnGapMs { get; init; } = 50;
    public int ReadBufferSize { get; init; } = 4096;
    public int WriteBufferSize { get; init; } = 4096;
}

public sealed record MulticastTransportOptions : TransportOptions
{
    public string GroupAddress { get; init; } = string.Empty;
    public int Port { get; init; }
    public string? LocalInterface { get; init; }
    public int Ttl { get; init; } = 1;
    public bool Loopback { get; init; }
}
