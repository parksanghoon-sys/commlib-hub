using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// Provides a placeholder length-prefixed protocol implementation.
/// </summary>
public sealed class LengthPrefixedProtocol : IProtocol
{
    /// <summary>
    /// Gets the protocol name exposed to the rest of the system.
    /// </summary>
    public string Name => "LengthPrefixed";
}
