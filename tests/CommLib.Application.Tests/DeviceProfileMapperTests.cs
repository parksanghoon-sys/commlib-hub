using System.Text.Json;
using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using Xunit;

namespace CommLib.Application.Tests;

public sealed class DeviceProfileMapperTests
{
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
