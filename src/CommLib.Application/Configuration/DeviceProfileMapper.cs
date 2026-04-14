using System.Text.Json;
using CommLib.Domain.Configuration;

namespace CommLib.Application.Configuration;

/// <summary>
/// 원시 JSON 기반 장치 설정을 강한 형식의 런타임 프로필로 변환합니다.
/// </summary>
public static class DeviceProfileMapper
{
    /// <summary>
    /// 원시 장치 프로필을 검증 가능한 전송별 프로필 객체 그래프로 변환합니다.
    /// </summary>
    /// <param name="raw">설정에서 읽어온 원시 장치 프로필입니다.</param>
    /// <returns>강한 형식의 장치 프로필 인스턴스입니다.</returns>
    public static DeviceProfile Map(DeviceProfileRaw raw)
    {
        var transportType = raw.Transport.GetProperty("Type").GetString();

        TransportOptions transport = transportType switch
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
    /// 지정한 전송 JSON을 특정 전송 옵션 형식으로 역직렬화합니다.
    /// </summary>
    /// <typeparam name="T">역직렬화할 전송 옵션 형식입니다.</typeparam>
    /// <param name="element">역직렬화할 JSON 요소입니다.</param>
    /// <returns>역직렬화된 전송 옵션 인스턴스입니다.</returns>
    private static T Deserialize<T>(JsonElement element)
    {
        return element.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
    }
}
