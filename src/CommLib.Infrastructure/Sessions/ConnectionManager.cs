using CommLib.Application.Sessions;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Sessions;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly ITransportFactory _transportFactory;
    private readonly Dictionary<string, IDeviceSession> _sessions = new();

    public ConnectionManager(ITransportFactory transportFactory)
    {
        _transportFactory = transportFactory;
    }

    public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        _ = _transportFactory.Create(profile.Transport);
        _sessions[profile.DeviceId] = new DeviceSession(profile.DeviceId);
        return Task.CompletedTask;
    }

    public IDeviceSession? GetSession(string deviceId)
    {
        _sessions.TryGetValue(deviceId, out var session);
        return session;
    }
}
