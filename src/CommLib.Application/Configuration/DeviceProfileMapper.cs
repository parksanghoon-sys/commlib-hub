using System.Text.Json;
using CommLib.Domain.Configuration;

namespace CommLib.Application.Configuration;

/// <summary>
/// Maps raw JSON-backed device configuration into strongly typed runtime profiles.
/// </summary>
public static class DeviceProfileMapper
{
    /// <summary>
    /// Converts a raw device profile into a validated transport-specific profile object graph.
    /// </summary>
    /// <param name="raw">The raw device profile loaded from configuration.</param>
    /// <returns>A strongly typed device profile instance.</returns>
    public static DeviceProfile Map(DeviceProfileRaw raw)
    {
        var transportType = raw.Transport.GetProperty("Type").GetString();

        var transport = transportType switch
        {
            "TcpClient" => Deserialize<TcpClientTransportOptions>(raw.Transport),
            "Udp" => Deserialize<UdpTransportOptions>(raw.Transport),
            "Serial" => Deserialize<SerialTransportOptions>(raw.Transport),
            "Multicast" => Deserialize<MulticastTransportOptions>(raw.Transport),
            _ => throw new InvalidOperationException($"Unknown transport type: {transportType}")
        };

        return new DeviceProfile
        {
            DeviceId = raw.DeviceId,
            DisplayName = raw.DisplayName,
            Enabled = raw.Enabled,
            Transport = transport,
            Protocol = raw.Protocol,
            Serializer = raw.Serializer,
            Reconnect = raw.Reconnect,
            RequestResponse = raw.RequestResponse
        };
    }

    /// <summary>
    /// Deserializes the supplied transport JSON into a specific transport options type.
    /// </summary>
    /// <typeparam name="T">The transport options type to deserialize.</typeparam>
    /// <param name="element">The JSON element to deserialize.</param>
    /// <returns>A deserialized transport options instance.</returns>
    private static T Deserialize<T>(JsonElement element)
    {
        return element.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
    }
}
