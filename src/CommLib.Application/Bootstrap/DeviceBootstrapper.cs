using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Bootstrap;

public sealed class DeviceBootstrapper
{
    private readonly IConnectionManager _connectionManager;

    public DeviceBootstrapper(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task StartAsync(IEnumerable<DeviceProfile> profiles, CancellationToken cancellationToken = default)
    {
        foreach (var profile in profiles.Where(static p => p.Enabled))
        {
            await _connectionManager.ConnectAsync(profile, cancellationToken);
        }
    }
}
