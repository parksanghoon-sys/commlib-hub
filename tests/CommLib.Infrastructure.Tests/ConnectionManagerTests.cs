using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// Verifies connection manager session registration behavior.
/// </summary>
public sealed class ConnectionManagerTests
{
    /// <summary>
    /// Ensures connecting a device profile registers a session retrievable by device identifier.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_CreatesSessionAccessibleByDeviceId()
    {
        var manager = new ConnectionManager(new TransportFactory());
        var profile = new DeviceProfile
        {
            DeviceId = "device-1",
            DisplayName = "Device 1",
            Enabled = true,
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = "127.0.0.1",
                Port = 502
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        await manager.ConnectAsync(profile);

        var session = manager.GetSession("device-1");

        Assert.NotNull(session);
        Assert.Equal("device-1", session.DeviceId);
    }

    /// <summary>
    /// Ensures unknown device identifiers return no session.
    /// </summary>
    [Fact]
    public void GetSession_UnknownDevice_ReturnsNull()
    {
        var manager = new ConnectionManager(new TransportFactory());

        var session = manager.GetSession("missing-device");

        Assert.Null(session);
    }
}
