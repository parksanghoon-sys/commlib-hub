namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents framing and protocol behavior options for a device.
/// </summary>
public sealed class ProtocolOptions
{
    /// <summary>
    /// Gets the protocol implementation name.
    /// </summary>
    public string Type { get; init; } = "LengthPrefixed";
    /// <summary>
    /// Gets the maximum frame length accepted by the protocol.
    /// </summary>
    public int MaxFrameLength { get; init; } = 65536;
    /// <summary>
    /// Gets a value indicating whether CRC validation is enabled.
    /// </summary>
    public bool UseCrc { get; init; } = true;
    /// <summary>
    /// Gets the optional start-of-text marker byte.
    /// </summary>
    public byte? Stx { get; init; }
    /// <summary>
    /// Gets the optional end-of-text marker byte.
    /// </summary>
    public byte? Etx { get; init; }
}
