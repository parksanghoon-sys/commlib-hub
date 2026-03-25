namespace CommLib.Domain.Configuration;

public sealed class DeviceProfile
{
    public required string DeviceId { get; init; }
    public required string DisplayName { get; init; }
    public bool Enabled { get; init; } = true;
    public required TransportOptions Transport { get; init; }
    public required ProtocolOptions Protocol { get; init; }
    public required SerializerOptions Serializer { get; init; }
    public ReconnectOptions Reconnect { get; init; } = new();
    public RequestResponseOptions RequestResponse { get; init; } = new();
}
