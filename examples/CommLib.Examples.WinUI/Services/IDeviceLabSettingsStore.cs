using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

public interface IDeviceLabSettingsStore
{
    string FilePath { get; }

    Task<DeviceLabAppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(DeviceLabAppSettings settings, CancellationToken cancellationToken = default);
}
