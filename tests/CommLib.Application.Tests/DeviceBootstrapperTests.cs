using CommLib.Application.Bootstrap;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Application.Tests;

/// <summary>
/// Verifies bootstrap behavior for enabled and disabled device profiles.
/// </summary>
public sealed class DeviceBootstrapperTests
{
    /// <summary>
    /// Ensures bootstrap connects only profiles marked as enabled.
    /// </summary>
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

    /// <summary>
    /// Provides a minimal in-memory connection manager used for bootstrap testing.
    /// </summary>
    private sealed class FakeConnectionManager : IConnectionManager
    {
        /// <summary>
        /// Gets the list of device identifiers passed to <see cref="ConnectAsync(DeviceProfile, CancellationToken)"/>.
        /// </summary>
        public List<string> ConnectedIds { get; } = new();

        /// <summary>
        /// Records the connected device identifier.
        /// </summary>
        /// <param name="profile">The profile passed by the bootstrapper.</param>
        /// <param name="cancellationToken">A token that could cancel the operation.</param>
        /// <returns>A completed task.</returns>
        public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
        {
            ConnectedIds.Add(profile.DeviceId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns no active session for the fake implementation.
        /// </summary>
        /// <param name="deviceId">The requested device identifier.</param>
        /// <returns>Always <see langword="null"/>.</returns>
        public IDeviceSession? GetSession(string deviceId) => null;
    }
}
