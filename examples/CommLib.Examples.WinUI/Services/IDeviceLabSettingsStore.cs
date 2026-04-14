using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// IDeviceLabSettingsStore 계약을 정의하는 인터페이스입니다.
/// </summary>
public interface IDeviceLabSettingsStore
{
    /// <summary>
    /// 설정 파일 경로를 가져옵니다.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// 저장된 설정을 읽어옵니다.
    /// </summary>
    Task<DeviceLabAppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 현재 설정을 저장합니다.
    /// </summary>
    Task SaveAsync(DeviceLabAppSettings settings, CancellationToken cancellationToken = default);
}
