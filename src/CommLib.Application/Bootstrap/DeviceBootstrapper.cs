using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Bootstrap;

/// <summary>
/// 애플리케이션 시작 시 활성화된 장치 프로필을 순서대로 연결합니다.
/// </summary>
public sealed class DeviceBootstrapper
{
    /// <summary>
    /// 장치 연결과 세션 등록을 수행하는 연결 관리자입니다.
    /// </summary>
    private readonly IConnectionManager _connectionManager;

    /// <summary>
    /// <see cref="DeviceBootstrapper"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="connectionManager">장치 연결을 수행할 연결 관리자입니다.</param>
    public DeviceBootstrapper(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// 활성화된 장치 프로필을 모두 연결합니다.
    /// </summary>
    /// <param name="profiles">검사하고 연결할 장치 프로필 목록입니다.</param>
    /// <param name="cancellationToken">부트스트랩 작업 취소 토큰입니다.</param>
    /// <returns>모든 활성 프로필 처리가 끝나면 완료되는 작업입니다.</returns>
    public async Task StartAsync(IEnumerable<DeviceProfile> profiles, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        foreach (var profile in profiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!profile.Enabled)
            {
                continue;
            }

            // 기존 fail-fast 계약은 유지하되, 실제 연결 부작용 전에 동일한 검증을 강제합니다.
            DeviceProfileValidator.ValidateAndThrow(profile);
            await _connectionManager.ConnectAsync(profile, cancellationToken);
        }
    }

    /// <summary>
    /// 활성화된 장치 프로필을 모두 시작하고, 실패한 항목은 중단하지 않고 결과로 수집합니다.
    /// </summary>
    /// <param name="profiles">검사하고 연결할 장치 프로필 목록입니다.</param>
    /// <param name="cancellationToken">부트스트랩 작업 취소 토큰입니다.</param>
    /// <returns>성공/실패를 함께 담은 부트스트랩 결과입니다.</returns>
    public async Task<DeviceBootstrapReport> StartWithReportAsync(
        IEnumerable<DeviceProfile> profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var connectedDeviceIds = new List<string>();
        var failures = new List<DeviceBootstrapFailure>();

        foreach (var profile in profiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!profile.Enabled)
            {
                continue;
            }

            try
            {
                // 보고서 경로도 연결 진입점과 같은 규칙으로 먼저 검증한 뒤 연결을 시도합니다.
                DeviceProfileValidator.ValidateAndThrow(profile);
                await _connectionManager.ConnectAsync(profile, cancellationToken).ConfigureAwait(false);
                connectedDeviceIds.Add(profile.DeviceId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // 호출자 취소가 아닌 실패는 전체 중단 대신 결과에 누적합니다.
                failures.Add(new DeviceBootstrapFailure(profile.DeviceId, exception));
            }
        }

        return new DeviceBootstrapReport(connectedDeviceIds, failures);
    }
}
