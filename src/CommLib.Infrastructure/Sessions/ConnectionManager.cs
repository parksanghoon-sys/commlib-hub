using CommLib.Application.Sessions;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Sessions;

/// <summary>
/// Manages transport creation and in-memory session registration for connected devices.
/// </summary>
public sealed class ConnectionManager : IConnectionManager
{
    /// <summary>
    /// Stores the transport factory used to initialize device transports.
    /// </summary>
    private readonly ITransportFactory _transportFactory;
    /// <summary>
    /// Stores active sessions indexed by device identifier.
    /// </summary>
    private readonly Dictionary<string, IDeviceSession> _sessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
    /// </summary>
    /// <param name="transportFactory">The factory used to create transports for device profiles.</param>
    public ConnectionManager(ITransportFactory transportFactory)
    {
        _transportFactory = transportFactory;
    }

    /// <summary>
    /// Creates the transport for the supplied device profile and registers a session for it.
    /// </summary>
    /// <param name="profile">The device profile to connect.</param>
    /// <param name="cancellationToken">A token that can cancel the connection process.</param>
    /// <returns>A completed task once the session has been registered.</returns>
    public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
    {
        _ = _transportFactory.Create(profile.Transport);
        _sessions[profile.DeviceId] = new DeviceSession(profile.DeviceId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets an active session by device identifier.
    /// </summary>
    /// <param name="deviceId">The identifier of the device session to retrieve.</param>
    /// <returns>The active session, or <see langword="null"/> if no session is registered.</returns>
    public IDeviceSession? GetSession(string deviceId)
    {
        _sessions.TryGetValue(deviceId, out var session);
        return session;
    }
}
