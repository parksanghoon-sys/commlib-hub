namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents the validated runtime configuration for a single device endpoint.
/// </summary>
public sealed class DeviceProfile
{
    /// <summary>
    /// Gets the unique identifier of the device.
    /// </summary>
    public required string DeviceId { get; init; }
    /// <summary>
    /// Gets the display name shown to operators or logs.
    /// </summary>
    public required string DisplayName { get; init; }
    /// <summary>
    /// Gets a value indicating whether the device should be started during bootstrap.
    /// </summary>
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// Gets the validated transport configuration for the device.
    /// </summary>
    public required TransportOptions Transport { get; init; }
    /// <summary>
    /// Gets the protocol settings applied to messages for this device.
    /// </summary>
    public required ProtocolOptions Protocol { get; init; }
    /// <summary>
    /// Gets the serializer settings used to encode outbound messages.
    /// </summary>
    public required SerializerOptions Serializer { get; init; }
    /// <summary>
    /// Gets the reconnect policy for recovering lost connections.
    /// </summary>
    public ReconnectOptions Reconnect { get; init; } = new();
    /// <summary>
    /// Gets request/response flow control settings for this device.
    /// </summary>
    public RequestResponseOptions RequestResponse { get; init; } = new();
}
