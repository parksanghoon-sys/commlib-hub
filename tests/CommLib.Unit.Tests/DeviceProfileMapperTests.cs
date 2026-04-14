using System.Text.Json;
using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 원시 장치 프로필이 강한 형식의 프로필로 매핑되는지 검증합니다.
/// </summary>
public sealed class DeviceProfileMapperTests
{
    /// <summary>
    /// TCP 클라이언트 전송 JSON이 구체적인 TCP 전송 옵션 형식으로 매핑되는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Map_TcpClientTransport_ReturnsConcreteTransport 작업을 수행합니다.
    /// </summary>
    public void Map_TcpClientTransport_ReturnsConcreteTransport()
    {
        var json = "{ \"Type\": \"TcpClient\", \"Host\": \"127.0.0.1\", \"Port\": 9000 }";

        var raw = new DeviceProfileRaw
        {
            DeviceId = "dev-01",
            DisplayName = "Device 01",
            Transport = JsonSerializer.Deserialize<JsonElement>(json)
        };

        var profile = DeviceProfileMapper.Map(raw);

        Assert.IsType<TcpClientTransportOptions>(profile.Transport);
    }

    /// <summary>
    /// 알 수 없는 전송 구분자 값은 거부되는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Map_UnknownTransport_Throws 작업을 수행합니다.
    /// </summary>
    public void Map_UnknownTransport_Throws()
    {
        var json = "{ \"Type\": \"UnknownX\" }";

        var raw = new DeviceProfileRaw
        {
            DeviceId = "dev-01",
            DisplayName = "Device 01",
            Transport = JsonSerializer.Deserialize<JsonElement>(json)
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileMapper.Map(raw));
    }

    /// <summary>
    /// 전송 외 공통 설정이 매핑 결과에 그대로 유지되는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Map_PreservesCommonOptions 작업을 수행합니다.
    /// </summary>
    public void Map_PreservesCommonOptions()
    {
        var json = "{ \"Type\": \"TcpClient\", \"Host\": \"127.0.0.1\", \"Port\": 9000 }";
        var protocol = new ProtocolOptions { Type = "LengthPrefixed", MaxFrameLength = 4096 };
        var serializer = new SerializerOptions
        {
            Type = "Json",
            BitFieldSchema = new BitFieldPayloadSchema
            {
                PayloadLengthBytes = 2,
                Fields = new[]
                {
                    new BitFieldPayloadField { Name = "mode", BitOffset = 0, BitLength = 3 }
                }
            }
        };
        var reconnect = new ReconnectOptions { Type = "Linear", IntervalMs = 1500, MaxAttempts = 5 };
        var requestResponse = new RequestResponseOptions { MaxPendingRequests = 32, DefaultTimeoutMs = 2500 };

        var raw = new DeviceProfileRaw
        {
            DeviceId = "dev-02",
            DisplayName = "Device 02",
            Enabled = false,
            Transport = JsonSerializer.Deserialize<JsonElement>(json),
            Protocol = protocol,
            Serializer = serializer,
            Reconnect = reconnect,
            RequestResponse = requestResponse
        };

        var profile = DeviceProfileMapper.Map(raw);

        Assert.Equal("dev-02", profile.DeviceId);
        Assert.Equal("Device 02", profile.DisplayName);
        Assert.False(profile.Enabled);
        Assert.Same(protocol, profile.Protocol);
        Assert.Same(serializer, profile.Serializer);
        Assert.Same(serializer.BitFieldSchema, profile.Serializer.BitFieldSchema);
        Assert.Same(reconnect, profile.Reconnect);
        Assert.Same(requestResponse, profile.RequestResponse);
    }
}
