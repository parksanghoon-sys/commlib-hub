using System.Text.Json;

namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents the raw device configuration read directly from JSON input.
/// </summary>
public sealed class DeviceProfileRaw
{
    /// <summary>
    /// Gets the unique identifier of the device.
    /// </summary>
    public string DeviceId { get; init; } = string.Empty;
    /// <summary>
    /// Gets the display name supplied by configuration.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>
    /// Gets a value indicating whether the device is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;
    /// <summary>
    /// Gets the raw JSON payload for the transport section before transport-specific mapping.
    /// </summary>
    public JsonElement Transport { get; init; }
    /// <summary>
    /// Gets the protocol configuration section.
    /// </summary>
    public ProtocolOptions Protocol { get; init; } = new();
    /// <summary>
    /// Gets the serializer configuration section.
    /// </summary>
    public SerializerOptions Serializer { get; init; } = new();
    /// <summary>
    /// Gets the reconnect configuration section.
    /// </summary>
    public ReconnectOptions Reconnect { get; init; } = new();
    /// <summary>
    /// Gets the request/response configuration section.
    /// </summary>
    public RequestResponseOptions RequestResponse { get; init; } = new();
}
