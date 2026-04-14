using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

public interface IDeviceLabSessionService : IAsyncDisposable
{
    event EventHandler<LogEntry>? LogEmitted;

    event EventHandler<ConnectionStateSnapshot>? ConnectionStateChanged;

    Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task SendAsync(IMessage message, CancellationToken cancellationToken = default);
}
