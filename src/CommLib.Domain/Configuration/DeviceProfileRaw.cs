using System.Text.Json;

namespace CommLib.Domain.Configuration;

public sealed class DeviceProfileRaw
{
    public string DeviceId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public JsonElement Transport { get; init; }
    public ProtocolOptions Protocol { get; init; } = new();
    public SerializerOptions Serializer { get; init; } = new();
    public ReconnectOptions Reconnect { get; init; } = new();
    public RequestResponseOptions RequestResponse { get; init; } = new();
}
