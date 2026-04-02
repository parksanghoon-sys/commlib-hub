using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Bootstrap;

/// <summary>
/// 애플리케이션 부트스트랩 시 활성화된 장치 연결을 시작합니다.
/// </summary>
public sealed class DeviceBootstrapper
{
    /// <summary>
    /// 장치 세션을 연결할 때 사용하는 연결 관리자를 저장합니다.
    /// </summary>
    private readonly IConnectionManager _connectionManager;

    /// <summary>
    /// <see cref="DeviceBootstrapper"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="connectionManager">장치 프로필 연결에 사용할 연결 관리자입니다.</param>
    public DeviceBootstrapper(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// 활성화된 장치 프로필을 모두 연결합니다.
    /// </summary>
    /// <param name="profiles">검사하고 연결할 장치 프로필 목록입니다.</param>
    /// <param name="cancellationToken">부트스트랩 작업을 취소하는 토큰입니다.</param>
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

            await _connectionManager.ConnectAsync(profile, cancellationToken);
        }
    }
}
