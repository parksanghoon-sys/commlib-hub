namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents the common configuration contract for all transport types.
/// </summary>
public abstract record TransportOptions
{
    /// <summary>
    /// Gets the discriminator value used to identify the transport implementation.
    /// </summary>
    public required string Type { get; init; }
}

/// <summary>
/// Represents TCP client transport settings for a device connection.
/// </summary>
public sealed record TcpClientTransportOptions : TransportOptions
{
    /// <summary>
    /// Gets the remote host name or IP address to connect to.
    /// </summary>
    public string Host { get; init; } = string.Empty;
    /// <summary>
    /// Gets the remote TCP port.
    /// </summary>
    public int Port { get; init; }
    /// <summary>
    /// Gets the connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMs { get; init; } = 3000;
    /// <summary>
    /// Gets the socket buffer size in bytes.
    /// </summary>
    public int BufferSize { get; init; } = 8192;
    /// <summary>
    /// Gets a value indicating whether the Nagle algorithm is disabled.
    /// </summary>
    public bool NoDelay { get; init; } = true;
}

/// <summary>
/// Represents UDP transport settings for datagram-based communication.
/// </summary>
public sealed record UdpTransportOptions : TransportOptions
{
    /// <summary>
    /// Gets the local UDP port to bind to.
    /// </summary>
    public int LocalPort { get; init; }
    /// <summary>
    /// Gets the optional remote host name or IP address used for directed sends.
    /// </summary>
    public string? RemoteHost { get; init; }
    /// <summary>
    /// Gets the optional remote UDP port used for directed sends.
    /// </summary>
    public int? RemotePort { get; init; }
}

/// <summary>
/// Represents serial port transport settings.
/// </summary>
public sealed record SerialTransportOptions : TransportOptions
{
    /// <summary>
    /// Gets the operating system serial port name.
    /// </summary>
    public string PortName { get; init; } = string.Empty;
    /// <summary>
    /// Gets the baud rate used for serial communication.
    /// </summary>
    public int BaudRate { get; init; } = 9600;
    /// <summary>
    /// Gets the number of data bits in each frame.
    /// </summary>
    public int DataBits { get; init; } = 8;
    /// <summary>
    /// Gets the parity configuration name.
    /// </summary>
    public string Parity { get; init; } = "None";
    /// <summary>
    /// Gets the stop bits configuration name.
    /// </summary>
    public string StopBits { get; init; } = "One";
    /// <summary>
    /// Gets a value indicating whether the serial line is operated in half-duplex mode.
    /// </summary>
    public bool HalfDuplex { get; init; }
    /// <summary>
    /// Gets the turnaround gap in milliseconds between half-duplex send and receive phases.
    /// </summary>
    public int TurnGapMs { get; init; } = 50;
    /// <summary>
    /// Gets the serial read buffer size in bytes.
    /// </summary>
    public int ReadBufferSize { get; init; } = 4096;
    /// <summary>
    /// Gets the serial write buffer size in bytes.
    /// </summary>
    public int WriteBufferSize { get; init; } = 4096;
}

/// <summary>
/// Represents multicast transport settings for group communication.
/// </summary>
public sealed record MulticastTransportOptions : TransportOptions
{
    /// <summary>
    /// Gets the multicast group IP address.
    /// </summary>
    public string GroupAddress { get; init; } = string.Empty;
    /// <summary>
    /// Gets the multicast port.
    /// </summary>
    public int Port { get; init; }
    /// <summary>
    /// Gets the optional local interface used to join the multicast group.
    /// </summary>
    public string? LocalInterface { get; init; }
    /// <summary>
    /// Gets the time-to-live value for outbound multicast packets.
    /// </summary>
    public int Ttl { get; init; } = 1;
    /// <summary>
    /// Gets a value indicating whether multicast loopback is enabled.
    /// </summary>
    public bool Loopback { get; init; }
}
