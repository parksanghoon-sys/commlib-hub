using System.Text.Json;
using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using Xunit;

namespace CommLib.Application.Tests;

/// <summary>
/// Verifies raw device profile mapping into strongly typed profiles.
/// </summary>
public sealed class DeviceProfileMapperTests
{
    /// <summary>
    /// Ensures TCP client transport JSON maps to the concrete TCP transport options type.
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
    /// Ensures unknown transport discriminator values are rejected.
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
