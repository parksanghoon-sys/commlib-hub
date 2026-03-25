using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// Represents a placeholder TCP transport implementation.
/// </summary>
public sealed class TcpTransport : ITransport
{
    /// <summary>
    /// Gets the transport name.
    /// </summary>
    public string Name => "TcpTransport";
}

/// <summary>
/// Represents a placeholder UDP transport implementation.
/// </summary>
public sealed class UdpTransport : ITransport
{
    /// <summary>
    /// Gets the transport name.
    /// </summary>
    public string Name => "UdpTransport";
}

/// <summary>
/// Represents a placeholder serial transport implementation.
/// </summary>
public sealed class SerialTransport : ITransport
{
    /// <summary>
    /// Gets the transport name.
    /// </summary>
    public string Name => "SerialTransport";
}

/// <summary>
/// Represents a placeholder multicast transport implementation.
/// </summary>
public sealed class MulticastTransport : ITransport
{
    /// <summary>
    /// Gets the transport name.
    /// </summary>
    public string Name => "MulticastTransport";
}
