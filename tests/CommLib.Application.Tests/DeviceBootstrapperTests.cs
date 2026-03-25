using CommLib.Application.Bootstrap;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Application.Tests;

public sealed class DeviceBootstrapperTests
{
    [Fact]
    public async Task StartAsync_ConnectsOnlyEnabledProfiles()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);

        var profiles = new[]
        {
            new DeviceProfile
            {
                DeviceId = "enabled-1",
                DisplayName = "Enabled 1",
                Enabled = true,
                Transport = new TcpClientTransportOptions { Type = "TcpClient", Host = "127.0.0.1", Port = 1000 },
                Protocol = new ProtocolOptions(),
                Serializer = new SerializerOptions()
            },
            new DeviceProfile
            {
                DeviceId = "disabled-1",
                DisplayName = "Disabled 1",
                Enabled = false,
                Transport = new TcpClientTransportOptions { Type = "TcpClient", Host = "127.0.0.1", Port = 1001 },
                Protocol = new ProtocolOptions(),
                Serializer = new SerializerOptions()
            }
        };

        await bootstrapper.StartAsync(profiles);

        Assert.Single(manager.ConnectedIds);
        Assert.Contains("enabled-1", manager.ConnectedIds);
    }

    private sealed class FakeConnectionManager : IConnectionManager
    {
        public List<string> ConnectedIds { get; } = new();

        public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
        {
            ConnectedIds.Add(profile.DeviceId);
            return Task.CompletedTask;
        }

        public IDeviceSession? GetSession(string deviceId) => null;
    }
}
