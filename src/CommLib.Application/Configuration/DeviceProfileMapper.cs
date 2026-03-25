using System.Text.Json;
using CommLib.Domain.Configuration;

namespace CommLib.Application.Configuration;

public static class DeviceProfileMapper
{
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

    private static T Deserialize<T>(JsonElement element)
    {
        return element.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
    }
}
