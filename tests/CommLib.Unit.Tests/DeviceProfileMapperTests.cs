using System.Text.Json;
using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
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
}
