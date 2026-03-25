using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Bootstrap;

/// <summary>
/// Starts enabled device connections during application bootstrap.
/// </summary>
public sealed class DeviceBootstrapper
{
    /// <summary>
    /// Stores the connection manager used to establish device sessions.
    /// </summary>
    private readonly IConnectionManager _connectionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceBootstrapper"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager used to connect device profiles.</param>
    public DeviceBootstrapper(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Connects all enabled device profiles.
    /// </summary>
    /// <param name="profiles">The device profiles to inspect and connect.</param>
    /// <param name="cancellationToken">A token that cancels the bootstrap operation.</param>
    /// <returns>A task that completes after all enabled profiles have been processed.</returns>
    public async Task StartAsync(IEnumerable<DeviceProfile> profiles, CancellationToken cancellationToken = default)
    {
        foreach (var profile in profiles.Where(static p => p.Enabled))
        {
            await _connectionManager.ConnectAsync(profile, cancellationToken);
        }
    }
}
