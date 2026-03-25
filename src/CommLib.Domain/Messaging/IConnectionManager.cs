using CommLib.Domain.Configuration;

namespace CommLib.Domain.Messaging;

public interface IConnectionManager
{
    Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default);
    IDeviceSession? GetSession(string deviceId);
}
