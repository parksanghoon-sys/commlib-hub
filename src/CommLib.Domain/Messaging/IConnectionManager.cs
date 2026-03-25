using CommLib.Domain.Configuration;

namespace CommLib.Domain.Messaging;

/// <summary>
/// Defines device connection lifecycle management operations.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Opens or initializes a connection for the specified device profile.
    /// </summary>
    /// <param name="profile">The validated device profile to connect.</param>
    /// <param name="cancellationToken">A token that cancels the connection attempt.</param>
    /// <returns>A task that completes when the connection attempt has been processed.</returns>
    Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets the active session associated with the specified device identifier.
    /// </summary>
    /// <param name="deviceId">The device identifier to search for.</param>
    /// <returns>The active device session, or <see langword="null"/> if none exists.</returns>
    IDeviceSession? GetSession(string deviceId);
}
